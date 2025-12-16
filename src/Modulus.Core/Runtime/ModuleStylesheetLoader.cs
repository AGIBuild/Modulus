using System;
using System.IO;
using System.Linq;
using Modulus.UI.Abstractions;

namespace Modulus.Core.Runtime;

/// <summary>
/// Helper for Blazor hosts to load module-provided CSS at runtime.
/// </summary>
public static class ModuleStylesheetLoader
{
    /// <summary>
    /// Attempts to load <c>module.css</c> from the package directory of the module owning the given route.
    /// Returns null when no owning module or no stylesheet is available.
    /// </summary>
    public static string? TryLoadCssForRoute(string route, IMenuRegistry menuRegistry, RuntimeContext runtimeContext)
    {
        if (string.IsNullOrWhiteSpace(route)) return null;

        var menu = menuRegistry.GetItems(MenuLocation.Main)
            .Concat(menuRegistry.GetItems(MenuLocation.Bottom))
            .FirstOrDefault(i => string.Equals(i.NavigationKey, route, StringComparison.OrdinalIgnoreCase));

        if (menu == null || string.IsNullOrWhiteSpace(menu.ModuleId)) return null;
        if (!runtimeContext.TryGetModuleHandle(menu.ModuleId, out var handle) || handle?.RuntimeModule == null) return null;

        var cssPath = Path.Combine(handle.RuntimeModule.PackagePath, "module.css");
        if (!File.Exists(cssPath)) return null;

        return File.ReadAllText(cssPath);
    }
}


