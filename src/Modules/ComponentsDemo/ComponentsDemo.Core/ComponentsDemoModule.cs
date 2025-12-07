using Microsoft.Extensions.DependencyInjection;
using Modulus.Modules.ComponentsDemo.ViewModels;
using Modulus.Sdk;

namespace Modulus.Modules.ComponentsDemo;

/// <summary>
/// Components Demo Module - demonstrates navigation and UI component features.
/// UI-specific implementations are in UI.Avalonia and UI.Blazor modules.
/// </summary>
[DependsOn()] // no explicit deps
[Module("ComponentsDemo", "Components Demo",
    Description = "Demonstrates navigation and UI components for both Avalonia and Blazor hosts.")]
public class ComponentsDemoModule : ModulusComponent
{
    public override void ConfigureServices(IModuleLifecycleContext context)
    {
        // Register ViewModels
        context.Services.AddTransient<ComponentsMainViewModel>();
        context.Services.AddTransient<NavigationDemoViewModel>();
        context.Services.AddTransient<BadgeDemoViewModel>();
        context.Services.AddTransient<DisabledDemoViewModel>();
        context.Services.AddTransient<HierarchyDemoViewModel>();
        context.Services.AddTransient<ContextMenuDemoViewModel>();
        context.Services.AddTransient<KeyboardDemoViewModel>();
        context.Services.AddTransient<LifecycleDemoViewModel>();
    }
}

