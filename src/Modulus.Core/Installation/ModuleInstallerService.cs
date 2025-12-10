using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Modulus.Core.Manifest;
using Modulus.Infrastructure.Data.Models;
using Modulus.Infrastructure.Data.Repositories;
using Modulus.Sdk;
using Modulus.UI.Abstractions;

namespace Modulus.Core.Installation;

public class ModuleInstallerService : IModuleInstallerService
{
    private readonly IModuleRepository _moduleRepository;
    private readonly IMenuRepository _menuRepository;
    private readonly IManifestValidator _manifestValidator;
    private readonly ILogger<ModuleInstallerService> _logger;

    public ModuleInstallerService(
        IModuleRepository moduleRepository,
        IMenuRepository menuRepository,
        IManifestValidator manifestValidator,
        ILogger<ModuleInstallerService> logger)
    {
        _moduleRepository = moduleRepository;
        _menuRepository = menuRepository;
        _manifestValidator = manifestValidator;
        _logger = logger;
    }

    public async Task InstallFromPathAsync(string packagePath, bool isSystem = false, string? hostType = null, CancellationToken cancellationToken = default)
    {
        var manifestPath = Path.Combine(packagePath, SystemModuleInstaller.VsixManifestFileName);

        if (!File.Exists(manifestPath))
        {
            throw new FileNotFoundException($"Manifest not found: {manifestPath}");
        }

        var manifest = await VsixManifestReader.ReadFromFileAsync(manifestPath, cancellationToken);
        if (manifest == null)
        {
            throw new InvalidOperationException($"Failed to read manifest from {manifestPath}");
        }

        var identity = manifest.Metadata.Identity;

        // Validate manifest
        var validationResult = await _manifestValidator.ValidateAsync(packagePath, manifestPath, manifest, hostType, cancellationToken);

        if (!validationResult.IsValid)
        {
            foreach (var error in validationResult.Errors)
            {
                _logger.LogError("Manifest validation error for {ModuleId}: {Error}", identity.Id, error);
            }
        }

        // Extract menus from manifest Assets (no assembly loading!)
        var menuAssets = manifest.Assets
            .Where(a => string.Equals(a.Type, ModulusAssetTypes.Menu, StringComparison.OrdinalIgnoreCase))
            .Where(a => string.IsNullOrEmpty(a.TargetHost) ||
                        (hostType != null && ModulusHostIds.Matches(a.TargetHost, hostType)))
            .ToList();

        var requestedBottom = menuAssets.Any(a =>
            string.Equals(a.Location, "Bottom", StringComparison.OrdinalIgnoreCase));
        var moduleLocation = (isSystem && requestedBottom) ? MenuLocation.Bottom : MenuLocation.Main;

        if (!isSystem && requestedBottom)
        {
            _logger.LogWarning("Module {ModuleId} requested Bottom menu location but is not system-managed. Forcing to Main.", identity.Id);
        }

        // Compute manifest hash for change detection
        var manifestHash = await VsixManifestReader.ComputeHashAsync(manifestPath, cancellationToken);

        // Prepare entities
        var moduleState = validationResult.IsValid
            ? Modulus.Infrastructure.Data.Models.ModuleState.Ready
            : Modulus.Infrastructure.Data.Models.ModuleState.Incompatible;

        var validationErrors = validationResult.IsValid
            ? null
            : JsonSerializer.Serialize(validationResult.Errors);

        var moduleEntity = new ModuleEntity
        {
            Id = identity.Id,
            DisplayName = manifest.Metadata.DisplayName,
            Version = identity.Version,
            Language = identity.Language,
            Publisher = identity.Publisher,
            Description = manifest.Metadata.Description,
            Tags = manifest.Metadata.Tags,
            Website = manifest.Metadata.MoreInfo,
            Path = Path.GetRelativePath(Directory.GetCurrentDirectory(), manifestPath),
            ManifestHash = manifestHash,
            ValidatedAt = DateTime.UtcNow,
            IsSystem = isSystem,
            IsEnabled = validationResult.IsValid,
            MenuLocation = moduleLocation,
            State = moduleState,
            ValidationErrors = validationErrors
        };

        var menuEntities = menuAssets.Select(asset => new MenuEntity
        {
            Id = $"{identity.Id}.{asset.Id}",
            ModuleId = identity.Id,
            DisplayName = asset.DisplayName ?? asset.Id ?? "Menu",
            Icon = asset.Icon ?? "Grid",
            Route = asset.Route ?? "",
            Location = moduleLocation,
            Order = asset.Order,
            ParentId = null
        }).ToList();

        // Persist
        _logger.LogInformation("Installing module {ModuleId} v{Version} to database...", identity.Id, identity.Version);

        await _moduleRepository.UpsertAsync(moduleEntity, cancellationToken);
        await _menuRepository.ReplaceModuleMenusAsync(identity.Id, menuEntities, cancellationToken);

        _logger.LogInformation("Module {ModuleId} installed successfully.", identity.Id);
    }

    public Task RegisterDevelopmentModuleAsync(string manifestPath, CancellationToken cancellationToken = default)
    {
        var dir = Path.GetDirectoryName(manifestPath);
        if (dir == null) throw new ArgumentException("Invalid manifest path");
        
        return InstallFromPathAsync(dir, isSystem: false, hostType: null, cancellationToken);
    }
}
