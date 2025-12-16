using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Modulus.Core.Manifest;
using Modulus.Core.Paths;
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
    private readonly IModuleCleanupService _cleanupService;
    private readonly ILogger<ModuleInstallerService> _logger;

    public ModuleInstallerService(
        IModuleRepository moduleRepository,
        IMenuRepository menuRepository,
        IManifestValidator manifestValidator,
        IModuleCleanupService cleanupService,
        ILogger<ModuleInstallerService> logger)
    {
        _moduleRepository = moduleRepository;
        _menuRepository = menuRepository;
        _manifestValidator = manifestValidator;
        _cleanupService = cleanupService;
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

        // Extract menus from module assembly attributes (metadata-only parsing)
        var menus = new List<MenuInfo>();
        var moduleLocation = MenuLocation.Main;

        if (hostType != null && validationResult.IsValid)
        {
            // Find host-specific UI package assembly (TargetHost MUST match current host).
            // NOTE: Do NOT fall back to host-agnostic Core packages; menus must be declared on host-specific entry types.
            var packageAsset = manifest.Assets
                .Where(a => string.Equals(a.Type, ModulusAssetTypes.Package, StringComparison.OrdinalIgnoreCase))
                .FirstOrDefault(a =>
                    !string.IsNullOrEmpty(a.Path) &&
                    !string.IsNullOrEmpty(a.TargetHost) &&
                    ModulusHostIds.Matches(a.TargetHost, hostType));

            if (packageAsset != null && !string.IsNullOrEmpty(packageAsset.Path))
            {
                var assemblyPath = Path.Combine(packagePath, packageAsset.Path);
                if (File.Exists(assemblyPath))
                {
                    try
                    {
                        menus = ModuleMenuAttributeReader.ReadMenus(assemblyPath, hostType).ToList();
                        
                        // Determine module location from menu attributes
                        var requestedBottom = menus.Any(m => m.Location == MenuLocation.Bottom);
                        moduleLocation = (isSystem && requestedBottom) ? MenuLocation.Bottom : MenuLocation.Main;

                        if (!isSystem && requestedBottom)
                        {
                            _logger.LogWarning("Module {ModuleId} requested Bottom menu location but is not system-managed. Forcing to Main.", identity.Id);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to read menu attributes from {AssemblyPath} for module {ModuleId}. Menus will be empty.", assemblyPath, identity.Id);
                    }
                }
                else
                {
                    _logger.LogWarning("Package assembly not found at {AssemblyPath} for module {ModuleId}. Menus will be empty.", assemblyPath, identity.Id);
                }
            }
            else
            {
                _logger.LogDebug("No host-specific package assembly found for {HostType} in module {ModuleId}. Menus will be empty.", hostType, identity.Id);
            }
        }

        // Compute manifest hash for change detection
        var manifestHash = await VsixManifestReader.ComputeHashAsync(manifestPath, cancellationToken);

        // Preserve existing IsEnabled state when updating
        var existingModule = await _moduleRepository.GetAsync(identity.Id, cancellationToken);
        // Preserve disabled state across updates, but do not keep a module enabled if validation fails.
        var preserveIsEnabled = existingModule?.IsEnabled == false ? false : validationResult.IsValid;

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
            Path = Path.GetRelativePath(AppContext.BaseDirectory, manifestPath),
            ManifestHash = manifestHash,
            ValidatedAt = DateTime.UtcNow,
            IsSystem = isSystem,
            IsEnabled = preserveIsEnabled,
            MenuLocation = moduleLocation,
            State = moduleState,
            ValidationErrors = validationErrors
        };

        // Menu ids MUST be: {ModuleId}.{HostType}.{Key}.{Index} (>= 4 parts) to align with runtime expectations.
        // Duplicates (same Key) are preserved with an incrementing Index for diagnostics.
        var menuEntities = new List<MenuEntity>();
        if (!string.IsNullOrWhiteSpace(hostType))
        {
            foreach (var group in menus.GroupBy(m => m.Key))
            {
                var idx = 0;
                foreach (var menu in group)
                {
                    var location = menu.Location;
                    if (!isSystem && location == MenuLocation.Bottom)
                        location = MenuLocation.Main;

                    menuEntities.Add(new MenuEntity
                    {
                        Id = $"{identity.Id}.{hostType}.{menu.Key}.{idx}",
                        ModuleId = identity.Id,
                        DisplayName = menu.DisplayName,
                        Icon = menu.Icon,
                        Route = menu.Route,
                        Location = location,
                        Order = menu.Order,
                        ParentId = null
                    });

                    idx++;
                }
            }
        }

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

    public async Task<ModuleInstallResult> InstallFromPackageAsync(string packagePath, bool overwrite = false, string? hostType = null, CancellationToken cancellationToken = default)
    {
        // Validate package file exists
        if (!File.Exists(packagePath))
        {
            return ModuleInstallResult.Failed($"Package file not found: {packagePath}");
        }

        if (!packagePath.EndsWith(".modpkg", StringComparison.OrdinalIgnoreCase))
        {
            return ModuleInstallResult.Failed("Invalid package file. Expected .modpkg extension.");
        }

        string? tempDir = null;
        try
        {
            // Extract to temp directory
            tempDir = Path.Combine(Path.GetTempPath(), $"modulus-install-{Guid.NewGuid():N}");
            Directory.CreateDirectory(tempDir);

            try
            {
                ZipFile.ExtractToDirectory(packagePath, tempDir);
            }
            catch (Exception ex)
            {
                return ModuleInstallResult.Failed($"Failed to extract package: {ex.Message}");
            }

            // Read manifest from extracted directory
            var manifestPath = Path.Combine(tempDir, SystemModuleInstaller.VsixManifestFileName);
            if (!File.Exists(manifestPath))
            {
                return ModuleInstallResult.Failed("Invalid package: extension.vsixmanifest not found.");
            }

            var manifest = await VsixManifestReader.ReadFromFileAsync(manifestPath, cancellationToken);
            if (manifest == null)
            {
                return ModuleInstallResult.Failed("Failed to read manifest from package.");
            }

            var identity = manifest.Metadata.Identity;

            // ===== CRITICAL: Check if same module ID exists in database =====
            var existingModule = await _moduleRepository.GetAsync(identity.Id, cancellationToken);
            string? existingModulePath = null;
            
            if (existingModule != null)
            {
                // Same module ID already exists
                if (existingModule.IsSystem)
                {
                    // System module - NEVER allow user to overwrite
                    return ModuleInstallResult.Failed(
                        $"Module '{existingModule.DisplayName}' is a system module and cannot be overwritten by user installation.");
                }
                
                // User module - require confirmation
                if (!overwrite)
                {
                    return ModuleInstallResult.ConfirmationRequired(identity.Id, manifest.Metadata.DisplayName);
                }
                
                // Get existing module's directory path for cleanup
                existingModulePath = Path.GetDirectoryName(
                    Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, existingModule.Path)));
            }

            // Determine target installation directory using module name (not GUID)
            var modulesRoot = Path.Combine(LocalStorage.GetUserRoot(), "Modules");
            var sanitizedName = SanitizeDirectoryName(manifest.Metadata.DisplayName ?? identity.Id);
            var targetDir = Path.Combine(modulesRoot, sanitizedName);
            
            // Check if directory exists but belongs to a different module
            var manifestInTarget = Path.Combine(targetDir, SystemModuleInstaller.VsixManifestFileName);
            if (Directory.Exists(targetDir) && File.Exists(manifestInTarget))
            {
                var existingManifest = await VsixManifestReader.ReadFromFileAsync(manifestInTarget, cancellationToken);
                if (existingManifest?.Metadata.Identity.Id != identity.Id)
                {
                    // Different module with same name - add random suffix
                    var randomSuffix = Guid.NewGuid().ToString("N")[..6];
                    targetDir = Path.Combine(modulesRoot, $"{sanitizedName}-{randomSuffix}");
                }
            }

            string? oldDirToCleanup = null;

            // Handle existing directory (could be from same module reinstall or directory conflict)
            if (Directory.Exists(targetDir))
            {
                // Overwrite: rename existing directory instead of deleting (DLLs may be locked)
                _logger.LogInformation("Overwriting existing module directory at {TargetDir}", targetDir);
                
                // Rename old directory to allow installation even if DLLs are locked
                var timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
                oldDirToCleanup = $"{targetDir}.old-{timestamp}";
                try
                {
                    Directory.Move(targetDir, oldDirToCleanup);
                    _logger.LogDebug("Renamed existing module directory to {OldDir}", oldDirToCleanup);
                }
                catch (Exception moveEx)
                {
                    _logger.LogWarning(moveEx, "Failed to rename existing module directory, attempting direct delete");
                    
                    // Fallback: Force GC and retry delete
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    await Task.Delay(100, cancellationToken);
                    
                    try
                    {
                        Directory.Delete(targetDir, true);
                    }
                    catch (Exception deleteEx)
                    {
                        return ModuleInstallResult.Failed($"Cannot replace existing module. Please restart the application and try again. ({deleteEx.Message})");
                    }
                }
            }
            
            // Also clean up old module path if different from target
            if (existingModulePath != null && existingModulePath != targetDir && Directory.Exists(existingModulePath))
            {
                var timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
                var oldModuleDir = $"{existingModulePath}.old-{timestamp}";
                try
                {
                    Directory.Move(existingModulePath, oldModuleDir);
                    _ = _cleanupService.ScheduleCleanupAsync(oldModuleDir, identity.Id);
                    _logger.LogDebug("Scheduled cleanup for old module path: {OldPath}", oldModuleDir);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to cleanup old module path: {OldPath}", existingModulePath);
                }
            }

            // Copy files to target directory
            Directory.CreateDirectory(targetDir);
            CopyDirectory(tempDir, targetDir);
            
            // Schedule cleanup of old directory (handles locked DLLs gracefully)
            if (oldDirToCleanup != null && Directory.Exists(oldDirToCleanup))
            {
                // Schedule cleanup - will retry or defer to next startup if files are locked
                // Pass moduleId so cleanup can be cancelled if module is reinstalled
                _ = _cleanupService.ScheduleCleanupAsync(oldDirToCleanup, identity.Id);
            }

            // Cancel any pending cleanup for this module (prevents accidental deletion on next startup)
            await _cleanupService.CancelCleanupByModuleIdAsync(identity.Id);
            await _cleanupService.CancelCleanupAsync(targetDir);

            // Register in database
            await InstallFromPathAsync(targetDir, isSystem: false, hostType: hostType, cancellationToken);

            _logger.LogInformation("Module {ModuleId} v{Version} installed from package to {Path}", 
                identity.Id, identity.Version, targetDir);

            return ModuleInstallResult.Succeeded(identity.Id, targetDir, manifest.Metadata.DisplayName, identity.Version);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to install module from package {PackagePath}", packagePath);
            return ModuleInstallResult.Failed($"Installation failed: {ex.Message}");
        }
        finally
        {
            // Cleanup temp directory
            if (tempDir != null && Directory.Exists(tempDir))
            {
                try
                {
                    Directory.Delete(tempDir, true);
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
        }
    }

    public async Task<ModuleInstallResult> InstallFromPackageStreamAsync(Stream packageStream, string fileName, bool overwrite = false, string? hostType = null, CancellationToken cancellationToken = default)
    {
        if (!fileName.EndsWith(".modpkg", StringComparison.OrdinalIgnoreCase))
        {
            return ModuleInstallResult.Failed("Invalid package file. Expected .modpkg extension.");
        }

        string? tempFile = null;
        try
        {
            // Save stream to temp file
            tempFile = Path.Combine(Path.GetTempPath(), $"modulus-upload-{Guid.NewGuid():N}.modpkg");
            await using (var fileStream = File.Create(tempFile))
            {
                await packageStream.CopyToAsync(fileStream, cancellationToken);
            }

            // Delegate to file-based installation
            return await InstallFromPackageAsync(tempFile, overwrite, hostType, cancellationToken);
        }
        finally
        {
            // Cleanup temp file
            if (tempFile != null && File.Exists(tempFile))
            {
                try
                {
                    File.Delete(tempFile);
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
        }
    }

    private static void CopyDirectory(string sourceDir, string targetDir)
    {
        Directory.CreateDirectory(targetDir);

        foreach (var file in Directory.GetFiles(sourceDir))
        {
            var targetFile = Path.Combine(targetDir, Path.GetFileName(file));
            File.Copy(file, targetFile, overwrite: true);
        }

        foreach (var dir in Directory.GetDirectories(sourceDir))
        {
            var targetSubDir = Path.Combine(targetDir, Path.GetFileName(dir));
            CopyDirectory(dir, targetSubDir);
        }
    }

    /// <summary>
    /// Sanitizes a module name for use as a directory name.
    /// Removes invalid characters and trims whitespace.
    /// </summary>
    private static string SanitizeDirectoryName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return "Module";
        }

        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = new string(name
            .Where(c => !invalidChars.Contains(c))
            .ToArray())
            .Trim()
            .Replace(' ', '-');

        // Remove consecutive dashes
        while (sanitized.Contains("--"))
        {
            sanitized = sanitized.Replace("--", "-");
        }

        // Ensure not empty after sanitization
        if (string.IsNullOrWhiteSpace(sanitized))
        {
            return "Module";
        }

        // Limit length
        if (sanitized.Length > 64)
        {
            sanitized = sanitized[..64];
        }

        return sanitized;
    }
}
