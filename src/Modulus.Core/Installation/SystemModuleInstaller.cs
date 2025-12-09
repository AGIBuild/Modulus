using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Modulus.Core.Manifest;
using Modulus.Infrastructure.Data.Repositories;

namespace Modulus.Core.Installation;

/// <summary>
/// Installs built-in system modules on first startup.
/// </summary>
public class SystemModuleInstaller
{
    /// <summary>
    /// Manifest file name (vsixmanifest format).
    /// </summary>
    public const string VsixManifestFileName = "extension.vsixmanifest";

    private readonly IModuleInstallerService _installer;
    private readonly IModuleRepository _moduleRepository;
    private readonly ILogger<SystemModuleInstaller> _logger;

    public SystemModuleInstaller(
        IModuleInstallerService installer,
        IModuleRepository moduleRepository,
        ILogger<SystemModuleInstaller> logger)
    {
        _installer = installer;
        _moduleRepository = moduleRepository;
        _logger = logger;
    }

    /// <summary>
    /// Ensures all built-in modules are installed. Call once at application startup.
    /// </summary>
    /// <param name="builtInModulePaths">Relative paths to built-in module directories.</param>
    /// <param name="hostType">The current host type (e.g., "Modulus.Host.Avalonia").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task EnsureBuiltInAsync(
        string[] builtInModulePaths,
        string? hostType = null,
        CancellationToken cancellationToken = default)
    {
        foreach (var relativePath in builtInModulePaths)
        {
            var fullPath = Path.Combine(AppContext.BaseDirectory, relativePath);
            await InstallIfNeededAsync(fullPath, isSystem: true, hostType, cancellationToken);
        }
    }

    /// <summary>
    /// Installs modules from a directory if they have a valid manifest.
    /// </summary>
    public async Task InstallFromDirectoryAsync(
        string modulesDirectory,
        bool isSystem = false,
        string? hostType = null,
        CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(modulesDirectory))
        {
            _logger.LogDebug("Modules directory not found at {Path}. Skipping.", modulesDirectory);
            return;
        }

        foreach (var moduleDir in Directory.GetDirectories(modulesDirectory))
        {
            await InstallIfNeededAsync(moduleDir, isSystem, hostType, cancellationToken);
        }
    }

    /// <summary>
    /// Installs a single module if needed (missing or version mismatch).
    /// </summary>
    private async Task InstallIfNeededAsync(
        string modulePath,
        bool isSystem,
        string? hostType,
        CancellationToken cancellationToken)
    {
        if (!Directory.Exists(modulePath))
        {
            _logger.LogDebug("Module directory {Path} does not exist. Skipping.", modulePath);
            return;
        }

        var manifestPath = Path.Combine(modulePath, VsixManifestFileName);

        if (!File.Exists(manifestPath))
        {
            _logger.LogDebug("No {ManifestFileName} found in {Path}. Skipping.", VsixManifestFileName, modulePath);
            return;
        }

        string? moduleId = null;
        string? moduleVersion = null;

        var manifest = await VsixManifestReader.ReadFromFileAsync(manifestPath, cancellationToken);
        if (manifest != null)
        {
            moduleId = manifest.Metadata.Identity.Id;
            moduleVersion = manifest.Metadata.Identity.Version;
        }

        if (string.IsNullOrEmpty(moduleId))
        {
            _logger.LogDebug("Invalid manifest in {Path}. Skipping.", modulePath);
            return;
        }

        try
        {
            var existing = await _moduleRepository.GetAsync(moduleId, cancellationToken);

            // Install if missing or version mismatch
            if (existing == null || existing.Version != moduleVersion)
            {
                _logger.LogInformation(
                    "Installing module {ModuleId} (Version {Version}, IsSystem={IsSystem})...",
                    moduleId, moduleVersion, isSystem);
                await _installer.InstallFromPathAsync(modulePath, isSystem, hostType, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to install module from {Path}", modulePath);
        }
    }
}
