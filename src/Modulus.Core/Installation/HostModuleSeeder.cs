using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Modulus.Infrastructure.Data.Models;
using Modulus.Infrastructure.Data.Repositories;
using Modulus.Sdk;
using Modulus.UI.Abstractions;

namespace Modulus.Core.Installation;

/// <summary>
/// Seeds/updates the Host application as a system module with its built-in menus.
/// Menus are declared via host-specific menu attributes and projected to DB at startup.
/// </summary>
public class HostModuleSeeder
{
    private readonly IModuleRepository _moduleRepository;
    private readonly IMenuRepository _menuRepository;
    private readonly ILogger<HostModuleSeeder> _logger;

    public HostModuleSeeder(
        IModuleRepository moduleRepository,
        IMenuRepository menuRepository,
        ILogger<HostModuleSeeder> logger)
    {
        _moduleRepository = moduleRepository;
        _menuRepository = menuRepository;
        _logger = logger;
    }

    /// <summary>
    /// Seeds/updates the Host module and its menus from menu attributes.
    /// </summary>
    public async Task SeedOrUpdateFromAttributesAsync(
        string hostModuleId,
        string displayName,
        string version,
        Type hostModuleType,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(hostModuleId)) throw new ArgumentException("Host module id is required.", nameof(hostModuleId));
        if (string.IsNullOrWhiteSpace(displayName)) throw new ArgumentException("DisplayName is required.", nameof(displayName));
        if (string.IsNullOrWhiteSpace(version)) throw new ArgumentException("Version is required.", nameof(version));
        if (hostModuleType == null) throw new ArgumentNullException(nameof(hostModuleType));

        _logger.LogInformation("Seeding/updating Host module {ModuleId}...", hostModuleId);

        // Create Host module entity
        var hostModule = new ModuleEntity
        {
            Id = hostModuleId,
            DisplayName = displayName,
            Version = version,
            Language = "en-US",
            Publisher = "Modulus Framework",
            Website = "https://github.com/AGIBuild/Modulus",
            Path = "built-in", // Special marker for host module
            IsSystem = true,
            IsEnabled = true,
            State = ModuleState.Ready,
            MenuLocation = MenuLocation.Main // Host menus can be in both Main and Bottom
        };

        await _moduleRepository.UpsertAsync(hostModule, cancellationToken);

        var menus = ResolveHostMenus(hostModuleId, hostModuleType);
        await _menuRepository.ReplaceModuleMenusAsync(hostModuleId, menus, cancellationToken);

        _logger.LogInformation("Host module {ModuleId} projected {MenuCount} menus.", hostModuleId, menus.Length);
    }

    private static MenuEntity[] ResolveHostMenus(string hostModuleId, Type hostModuleType)
    {
        // HostType is the same as ModuleId (e.g. Modulus.Host.Blazor).
        var hostType = hostModuleId;

        if (ModulusHostIds.Matches(hostType, ModulusHostIds.Blazor))
        {
            var attrs = (BlazorMenuAttribute[])Attribute.GetCustomAttributes(hostModuleType, typeof(BlazorMenuAttribute), inherit: false);
            return BuildMenuEntities(hostModuleId, hostType, attrs.Select(a => new MenuProjection(a.Key, a.DisplayName, a.Route)
            {
                Icon = a.Icon.ToString(),
                Location = a.Location,
                Order = a.Order
            }).ToList());
        }

        if (ModulusHostIds.Matches(hostType, ModulusHostIds.Avalonia))
        {
            var attrs = (AvaloniaMenuAttribute[])Attribute.GetCustomAttributes(hostModuleType, typeof(AvaloniaMenuAttribute), inherit: false);
            return BuildMenuEntities(hostModuleId, hostType, attrs.Select(a => new MenuProjection(a.Key, a.DisplayName, a.ViewModelType.FullName ?? a.ViewModelType.Name)
            {
                Icon = a.Icon.ToString(),
                Location = a.Location,
                Order = a.Order
            }).ToList());
        }

        return Array.Empty<MenuEntity>();
    }

    private static MenuEntity[] BuildMenuEntities(string moduleId, string hostType, List<MenuProjection> projections)
    {
        var entities = new List<MenuEntity>();
        foreach (var group in projections.GroupBy(p => p.Key))
        {
            var idx = 0;
            foreach (var p in group)
            {
                entities.Add(new MenuEntity
                {
                    Id = $"{moduleId}.{hostType}.{p.Key}.{idx}",
                    ModuleId = moduleId,
                    DisplayName = p.DisplayName,
                    Icon = p.Icon,
                    Route = p.Route,
                    Location = p.Location,
                    Order = p.Order,
                    ParentId = null
                });
                idx++;
            }
        }
        return entities.ToArray();
    }

    private sealed record MenuProjection(string Key, string DisplayName, string Route)
    {
        public string Icon { get; init; } = IconKind.Grid.ToString();
        public MenuLocation Location { get; init; } = MenuLocation.Main;
        public int Order { get; init; } = 50;
    }
}

