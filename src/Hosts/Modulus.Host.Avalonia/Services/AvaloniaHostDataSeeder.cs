using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Modulus.Core.Runtime;
using Modulus.Infrastructure.Data.Models;
using Modulus.Infrastructure.Data.Repositories;
using Modulus.Sdk;
using Modulus.UI.Abstractions;
using DbModuleState = Modulus.Infrastructure.Data.Models.ModuleState;

namespace Modulus.Host.Avalonia.Services;

/// <summary>
/// Seeds Avalonia-specific host data (modules and menus) from bundled-modules.json.
/// </summary>
public class AvaloniaHostDataSeeder : BundledModuleSeeder
{
    public override string HostType => ModulusHostIds.Avalonia;

    public AvaloniaHostDataSeeder(
        IModuleRepository moduleRepository,
        IMenuRepository menuRepository,
        ILogger<AvaloniaHostDataSeeder> logger)
        : base(moduleRepository, menuRepository, logger)
    {
    }

    /// <inheritdoc />
    protected override async Task SeedHostModuleAsync(CancellationToken cancellationToken)
    {
        var hostModuleId = ModulusHostIds.Avalonia;

        // Host module is always defined in code (not in bundled-modules.json)
        // because it represents the host application itself
        var hostModule = new BundledModuleDefinition
        {
            Id = hostModuleId,
            DisplayName = "Modulus Host (Avalonia)",
            Version = "1.0.0",
            Language = "en-US",
            Publisher = "Modulus Framework",
            Description = "Built-in host module for Avalonia platform",
            Website = "https://github.com/AGIBuild/Modulus",
            Path = "built-in",
            IsBundled = true,
            IsSystem = true,
            IsEnabled = true,
            MenuLocation = MenuLocation.Main,
            State = DbModuleState.Ready,
            SupportedHosts = [ModulusHostIds.Avalonia],
            MenusByHost = new Dictionary<string, IReadOnlyList<MenuDefinition>>
            {
                [ModulusHostIds.Avalonia] =
                [
                    new MenuDefinition
                    {
                        Id = $"{hostModuleId}.Modules",
                        DisplayName = "Extensions",
                        Icon = IconKind.AppsAddIn.ToString(),
                        Route = "Modulus.Host.Avalonia.Shell.ViewModels.ModuleListViewModel",
                        Location = MenuLocation.Main,
                        Order = 1000
                    },
                    new MenuDefinition
                    {
                        Id = $"{hostModuleId}.Settings",
                        DisplayName = "Settings",
                        Icon = IconKind.Settings.ToString(),
                        Route = "Modulus.Host.Avalonia.Shell.ViewModels.SettingsViewModel",
                        Location = MenuLocation.Bottom,
                        Order = 100
                    }
                ]
            }
        };

        await UpsertModuleWithMenusAsync(hostModule, cancellationToken);
    }
}
