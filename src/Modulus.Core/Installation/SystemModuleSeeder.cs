using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Modulus.Core.Manifest;
using Modulus.Infrastructure.Data.Repositories;

namespace Modulus.Core.Installation;

public class SystemModuleSeeder
{
    private readonly IModuleInstallerService _installer;
    private readonly IModuleRepository _moduleRepository;
    private readonly ILogger<SystemModuleSeeder> _logger;

    public SystemModuleSeeder(
        IModuleInstallerService installer,
        IModuleRepository moduleRepository,
        ILogger<SystemModuleSeeder> logger)
    {
        _installer = installer;
        _moduleRepository = moduleRepository;
        _logger = logger;
    }

    /// <summary>
    /// Scans a root directory containing multiple module folders.
    /// </summary>
    public async Task SeedFromDirectoryAsync(string systemModulesRoot, bool isSystem = true, CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(systemModulesRoot))
        {
            _logger.LogInformation("Modules directory not found at {Path}. Skipping seeding.", systemModulesRoot);
            return;
        }

        var moduleDirs = Directory.GetDirectories(systemModulesRoot);
        foreach (var dir in moduleDirs)
        {
            await SeedFromPathAsync(dir, isSystem, hostType: null, cancellationToken);
        }
    }

    /// <summary>
    /// Seeds a single module from its package path.
    /// </summary>
    /// <param name="modulePath">Path to module package directory containing manifest.json</param>
    /// <param name="isSystem">Whether this is a system module (cannot be uninstalled)</param>
    /// <param name="hostType">The current host type (e.g., "AvaloniaApp"). Only loads UI assemblies for this host.</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task SeedFromPathAsync(string modulePath, bool isSystem = true, string? hostType = null, CancellationToken cancellationToken = default)
    {
        var manifestPath = Path.Combine(modulePath, "manifest.json");
        if (!File.Exists(manifestPath)) return;

        try
        {
            var manifest = await ManifestReader.ReadFromFileAsync(manifestPath, cancellationToken);
            if (manifest == null) return;

            var existing = await _moduleRepository.GetAsync(manifest.Id, cancellationToken);
            
            // Install if missing or version mismatch
            if (existing == null || existing.Version != manifest.Version)
            {
                _logger.LogInformation("Seeding module {ModuleId} (Version {Version}, IsSystem={IsSystem})...", 
                    manifest.Id, manifest.Version, isSystem);
                await _installer.InstallFromPathAsync(modulePath, isSystem, hostType, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to seed module from {Path}", modulePath);
        }
    }
}
