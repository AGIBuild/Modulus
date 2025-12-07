using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Modulus.Infrastructure.Data.Models;
using Modulus.Infrastructure.Data.Repositories;
using Modulus.UI.Abstractions;

namespace Modulus.Core.Installation;

/// <summary>
/// Seeds the Host application as a system module with its built-in menus.
/// This ensures all menus come from the database (full database-driven approach).
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
    /// Seeds the Host module and its built-in menus if not already present.
    /// </summary>
    /// <param name="hostType">The host type (e.g., "Avalonia", "Blazor")</param>
    /// <param name="modulesViewModelType">Full type name of the Modules/Extensions ViewModel</param>
    /// <param name="settingsViewModelType">Full type name of the Settings ViewModel</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task SeedAsync(
        string hostType,
        string modulesViewModelType,
        string settingsViewModelType,
        CancellationToken cancellationToken = default)
    {
        var hostModuleId = $"Modulus.Host.{hostType}";
        
        var existing = await _moduleRepository.GetAsync(hostModuleId, cancellationToken);
        if (existing != null)
        {
            _logger.LogDebug("Host module {ModuleId} already exists, skipping seed.", hostModuleId);
            return;
        }

        _logger.LogInformation("Seeding Host module {ModuleId}...", hostModuleId);

        // Create Host module entity
        var hostModule = new ModuleEntity
        {
            Id = hostModuleId,
            Name = $"Modulus Host ({hostType})",
            Version = "1.0.0",
            Author = "Modulus Framework",
            Website = "https://github.com/AGIBuild/Modulus",
            Path = "built-in", // Special marker for host module
            IsSystem = true,
            IsEnabled = true,
            State = ModuleState.Ready,
            MenuLocation = MenuLocation.Main // Host menus can be in both Main and Bottom
        };

        await _moduleRepository.UpsertAsync(hostModule, cancellationToken);

        // Create built-in menus
        var menus = new[]
        {
            new MenuEntity
            {
                Id = $"{hostModuleId}.Modules",
                ModuleId = hostModuleId,
                DisplayName = "Extensions",
                Icon = IconKind.AppsAddIn.ToString(),
                Route = modulesViewModelType,
                Location = MenuLocation.Main,
                Order = 1000 // At the end of main menu
            },
            new MenuEntity
            {
                Id = $"{hostModuleId}.Settings",
                ModuleId = hostModuleId,
                DisplayName = "Settings",
                Icon = IconKind.Settings.ToString(),
                Route = settingsViewModelType,
                Location = MenuLocation.Bottom,
                Order = 100
            }
        };

        await _menuRepository.ReplaceModuleMenusAsync(hostModuleId, menus, cancellationToken);
        
        _logger.LogInformation("Host module {ModuleId} seeded with {MenuCount} menus.", hostModuleId, menus.Length);
    }
}

