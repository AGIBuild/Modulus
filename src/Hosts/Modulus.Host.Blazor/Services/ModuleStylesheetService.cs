using System;
using System.Threading.Tasks;
using Modulus.Core.Runtime;
using Modulus.UI.Abstractions;

namespace Modulus.Host.Blazor.Services;

/// <summary>
/// Loads and exposes module-provided CSS for Blazor host at runtime.
/// This avoids relying on Blazor CSS isolation/static web assets for dynamically loaded modules.
/// </summary>
public sealed class ModuleStylesheetService
{
    private readonly IMenuRegistry _menuRegistry;
    private readonly RuntimeContext _runtimeContext;

    public string? CurrentModuleId { get; private set; }
    public string? CurrentCss { get; private set; }

    public event EventHandler? Changed;

    public ModuleStylesheetService(IMenuRegistry menuRegistry, RuntimeContext runtimeContext)
    {
        _menuRegistry = menuRegistry;
        _runtimeContext = runtimeContext;
    }

    public Task UpdateForRouteAsync(string? route)
    {
        if (string.IsNullOrWhiteSpace(route))
        {
            Set(null, null);
            return Task.CompletedTask;
        }

        var menu = _menuRegistry.GetItems(MenuLocation.Main)
            .Concat(_menuRegistry.GetItems(MenuLocation.Bottom))
            .FirstOrDefault(i => string.Equals(i.NavigationKey, route, StringComparison.OrdinalIgnoreCase));

        var moduleId = menu?.ModuleId;
        if (string.IsNullOrWhiteSpace(moduleId))
        {
            Set(null, null);
            return Task.CompletedTask;
        }

        if (string.Equals(CurrentModuleId, moduleId, StringComparison.OrdinalIgnoreCase))
        {
            return Task.CompletedTask;
        }

        // Load module.css from the module package directory (no Blazor css isolation dependency).
        var css = ModuleStylesheetLoader.TryLoadCssForRoute(route, _menuRegistry, _runtimeContext);
        Set(moduleId, css);
        return Task.CompletedTask;
    }

    private void Set(string? moduleId, string? css)
    {
        CurrentModuleId = moduleId;
        CurrentCss = css;
        Changed?.Invoke(this, EventArgs.Empty);
    }
}


