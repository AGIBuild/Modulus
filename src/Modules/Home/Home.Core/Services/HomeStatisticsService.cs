using Microsoft.Extensions.DependencyInjection;
using Modulus.Core.Architecture;
using Modulus.Core.Runtime;
using Modulus.Infrastructure.Data.Repositories;
using Modulus.UI.Abstractions;
using RuntimeModuleState = Modulus.Core.Runtime.ModuleState;

namespace Modulus.Modules.Home.Services;

/// <summary>
/// Implementation of IHomeStatisticsService using RuntimeContext and IModuleRepository.
/// </summary>
public class HomeStatisticsService : IHomeStatisticsService
{
    private readonly RuntimeContext _runtimeContext;

    public HomeStatisticsService(RuntimeContext runtimeContext)
    {
        _runtimeContext = runtimeContext;
    }

    public async Task<HomeStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default)
    {
        // IMPORTANT:
        // Module services are created from the module's own DI container, which does NOT automatically
        // resolve host-registered services (IMenuRegistry/IModuleRepository, etc). To access host services,
        // we must use the module handle's CompositeServiceProvider (module + host).
        var moduleId = AssemblyDomainInfo.GetCurrentModuleId();
        if (string.IsNullOrWhiteSpace(moduleId) || !_runtimeContext.TryGetModuleHandle(moduleId, out var handle) || handle == null)
        {
            // If we can't resolve the composite provider, we can still show runtime module count/version,
            // but we won't have DB-backed installed count or navigation keys.
            return BuildStatsWithoutHostServices();
        }

        var compositeProvider = handle.CompositeServiceProvider;

        // Get installed modules from repository
        int installedCount = 0;
        using (var scope = compositeProvider.CreateScope())
        {
            var moduleRepo = scope.ServiceProvider.GetService<IModuleRepository>();
            if (moduleRepo != null)
            {
                var modules = await moduleRepo.GetAllAsync(cancellationToken);
                installedCount = modules.Count;
            }
        }

        // Get menu registry to find navigation keys
        var menuRegistry = compositeProvider.GetService<IMenuRegistry>();
        var allMenuItems = menuRegistry != null
            ? menuRegistry.GetItems(MenuLocation.Main)
                .Concat(menuRegistry.GetItems(MenuLocation.Bottom))
                .ToList()
            : new List<MenuItem>();

        // Get running modules from RuntimeContext
        var runtimeModules = _runtimeContext.RuntimeModules;
        var runningModules = runtimeModules
            .Where(m => m.State == RuntimeModuleState.Active)
            .Select(m =>
            {
                // Find navigation key from menu items (case-insensitive comparison)
                var menuItem = allMenuItems.FirstOrDefault(mi =>
                    string.Equals(mi.ModuleId, m.Descriptor.Id, StringComparison.OrdinalIgnoreCase));
                return new ModuleInfo
                {
                    Id = m.Descriptor.Id,
                    DisplayName = m.Descriptor.DisplayName,
                    Version = m.Descriptor.Version,
                    IsRunning = true,
                    NavigationKey = menuItem?.NavigationKey ?? ""
                };
            })
            .ToList();

        // Get framework version from assembly
        var frameworkVersion = typeof(RuntimeContext).Assembly.GetName().Version?.ToString() ?? "1.0.0";

        return new HomeStatistics
        {
            InstalledModuleCount = installedCount,
            RunningModuleCount = runningModules.Count,
            FrameworkVersion = frameworkVersion,
            HostType = _runtimeContext.HostType ?? "Unknown",
            RunningModules = runningModules
        };
    }

    private HomeStatistics BuildStatsWithoutHostServices()
    {
        var runtimeModules = _runtimeContext.RuntimeModules;
        var runningModules = runtimeModules
            .Where(m => m.State == RuntimeModuleState.Active)
            .Select(m => new ModuleInfo
            {
                Id = m.Descriptor.Id,
                DisplayName = m.Descriptor.DisplayName,
                Version = m.Descriptor.Version,
                IsRunning = true,
                NavigationKey = ""
            })
            .ToList();

        var frameworkVersion = typeof(RuntimeContext).Assembly.GetName().Version?.ToString() ?? "1.0.0";

        return new HomeStatistics
        {
            InstalledModuleCount = 0,
            RunningModuleCount = runningModules.Count,
            FrameworkVersion = frameworkVersion,
            HostType = _runtimeContext.HostType ?? "Unknown",
            RunningModules = runningModules
        };
    }
}
