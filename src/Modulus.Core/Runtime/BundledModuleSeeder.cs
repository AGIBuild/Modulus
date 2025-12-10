using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Modulus.Infrastructure.Data.Models;
using Modulus.Infrastructure.Data.Repositories;

namespace Modulus.Core.Runtime;

/// <summary>
/// Base class for seeding bundled modules from the embedded bundled-modules.json manifest.
/// </summary>
public abstract class BundledModuleSeeder : IHostDataSeeder
{
    private readonly IModuleRepository _moduleRepository;
    private readonly IMenuRepository _menuRepository;
    private readonly ILogger _logger;

    /// <summary>
    /// The host type identifier (e.g., "Modulus.Host.Avalonia").
    /// </summary>
    public abstract string HostType { get; }

    /// <summary>
    /// Gets the assembly containing the embedded bundled-modules.json resource.
    /// Override to provide a different assembly.
    /// </summary>
    protected virtual Assembly ResourceAssembly => GetType().Assembly;

    protected BundledModuleSeeder(
        IModuleRepository moduleRepository,
        IMenuRepository menuRepository,
        ILogger logger)
    {
        _moduleRepository = moduleRepository;
        _menuRepository = menuRepository;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Seeding {HostType} host data...", HostType);

        // 1. Seed Host module (defined by derived class)
        await SeedHostModuleAsync(cancellationToken);

        // 2. Load bundled modules from embedded JSON
        var manifest = BundledModulesManifest.LoadFromEmbeddedResource(ResourceAssembly);
        if (manifest == null)
        {
            _logger.LogWarning("No bundled-modules.json found in {Assembly}. Skipping bundled module seeding.", 
                ResourceAssembly.GetName().Name);
            return;
        }

        _logger.LogInformation("Loaded bundled modules manifest (generated: {GeneratedAt}, modules: {Count})",
            manifest.GeneratedAt, manifest.Modules.Count);

        // 3. Seed bundled modules
        foreach (var moduleJson in manifest.Modules)
        {
            // Check if this module supports current host
            if (moduleJson.SupportedHosts.Count > 0 && 
                !moduleJson.SupportedHosts.Contains(HostType))
            {
                _logger.LogDebug("Module {ModuleId} does not support {HostType}, skipping.",
                    moduleJson.Id, HostType);
                continue;
            }

            // Get menus for current host
            if (!moduleJson.Menus.TryGetValue(HostType, out var menus) || menus.Count == 0)
            {
                _logger.LogDebug("Module {ModuleId} has no menus for {HostType}, skipping.",
                    moduleJson.Id, HostType);
                continue;
            }

            var definition = moduleJson.ToDefinition();
            await UpsertModuleWithMenusAsync(definition, menus.ConvertAll(m => m.ToDefinition()), cancellationToken);
        }

        _logger.LogInformation("{HostType} host data seeding completed.", HostType);
    }

    /// <summary>
    /// Seeds the host module. Override to define host-specific module and menus.
    /// </summary>
    protected abstract Task SeedHostModuleAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Upserts a module with its menus.
    /// </summary>
    protected async Task UpsertModuleWithMenusAsync(
        BundledModuleDefinition module,
        IReadOnlyList<MenuDefinition> menus,
        CancellationToken cancellationToken)
    {
        var existing = await _moduleRepository.GetAsync(module.Id, cancellationToken);
        if (existing != null)
        {
            _logger.LogDebug("Module {ModuleId} already exists, skipping seed.", module.Id);
            return;
        }

        _logger.LogInformation("Seeding module {ModuleId} ({DisplayName})...", module.Id, module.DisplayName);

        // Insert module
        await _moduleRepository.UpsertAsync(module.ToEntity(), cancellationToken);

        // Insert menus
        var menuEntities = new MenuEntity[menus.Count];
        for (var i = 0; i < menus.Count; i++)
        {
            menuEntities[i] = menus[i].ToEntity(module.Id);
        }

        await _menuRepository.ReplaceModuleMenusAsync(module.Id, menuEntities, cancellationToken);
    }

    /// <summary>
    /// Helper to upsert module with inline menus (for host module).
    /// </summary>
    protected async Task UpsertModuleWithMenusAsync(
        BundledModuleDefinition module,
        CancellationToken cancellationToken)
    {
        // For modules with MenusByHost, get menus for current host
        if (module.MenusByHost.TryGetValue(HostType, out var menus))
        {
            await UpsertModuleWithMenusAsync(module, menus, cancellationToken);
        }
        else
        {
            // No menus, just insert module
            var existing = await _moduleRepository.GetAsync(module.Id, cancellationToken);
            if (existing == null)
            {
                _logger.LogInformation("Seeding module {ModuleId} ({DisplayName}) without menus...", 
                    module.Id, module.DisplayName);
                await _moduleRepository.UpsertAsync(module.ToEntity(), cancellationToken);
            }
        }
    }
}

