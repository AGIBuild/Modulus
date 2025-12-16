using System.Reflection;
using Modulus.Core.Installation;
using Modulus.Sdk;
using Modulus.UI.Abstractions;

namespace Modulus.Core.Tests.Installation;

public class ModuleMenuAttributeReaderTests
{
    [Fact]
    public void ReadMenus_BlazorHost_ReadsBlazorMenuAttributes()
    {
        var assemblyPath = Assembly.GetExecutingAssembly().Location;

        var menus = ModuleMenuAttributeReader.ReadMenus(assemblyPath, ModulusHostIds.Blazor);

        Assert.Contains(menus, m => m.Key == "test-blazor" && m.DisplayName == "Test Blazor" && m.Route == "/test-blazor");
        Assert.Contains(menus, m => m.Key == "dup" && m.DisplayName == "Dup 1" && m.Route == "/dup-1");
        Assert.Contains(menus, m => m.Key == "dup" && m.DisplayName == "Dup 2" && m.Route == "/dup-2");
    }

    [Fact]
    public void ReadMenus_AvaloniaHost_ReadsAvaloniaMenuAttributes()
    {
        var assemblyPath = Assembly.GetExecutingAssembly().Location;

        var menus = ModuleMenuAttributeReader.ReadMenus(assemblyPath, ModulusHostIds.Avalonia);

        Assert.Contains(menus, m => m.Key == "test-avalonia" && m.DisplayName == "Test Avalonia" && m.Route.EndsWith(nameof(DummyAvaloniaViewModel)));
    }

    [BlazorMenu("test-blazor", "Test Blazor", "/test-blazor", Icon = IconKind.Home, Order = 10, Location = MenuLocation.Main)]
    [BlazorMenu("dup", "Dup 1", "/dup-1", Order = 20)]
    [BlazorMenu("dup", "Dup 2", "/dup-2", Order = 21)]
    private sealed class DummyBlazorModule : ModulusPackage { }

    [AvaloniaMenu("test-avalonia", "Test Avalonia", typeof(DummyAvaloniaViewModel), Icon = IconKind.Grid, Order = 30)]
    private sealed class DummyAvaloniaModule : ModulusPackage { }

    private sealed class DummyAvaloniaViewModel { }
}


