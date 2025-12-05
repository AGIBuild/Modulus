using Avalonia.Headless.XUnit;
using Modulus.UI.Abstractions;
using Modulus.UI.Avalonia.Controls;

namespace Modulus.UI.Avalonia.Tests;

/// <summary>
/// Tests for SideNav control.
/// </summary>
public class SideNavTests
{
    [AvaloniaFact]
    public void SideNav_DefaultProperties()
    {
        var sideNav = new SideNav();

        Assert.Null(sideNav.MainItems);
        Assert.Null(sideNav.BottomItems);
        Assert.Null(sideNav.MainSelectedItem);
        Assert.Null(sideNav.BottomSelectedItem);
        Assert.False(sideNav.IsCollapsed);
    }

    [AvaloniaFact]
    public void SideNav_MainItems_CanBeSet()
    {
        var sideNav = new SideNav();
        var items = new List<MenuItem>
        {
            new MenuItem("home", "Home", IconKind.Home, "/home"),
            new MenuItem("settings", "Settings", IconKind.Settings, "/settings")
        };

        sideNav.MainItems = items;

        Assert.Equal(items, sideNav.MainItems);
    }

    [AvaloniaFact]
    public void SideNav_BottomItems_CanBeSet()
    {
        var sideNav = new SideNav();
        var items = new List<MenuItem>
        {
            new MenuItem("help", "Help", IconKind.Help, "/help"),
            new MenuItem("about", "About", IconKind.Info, "/about")
        };

        sideNav.BottomItems = items;

        Assert.Equal(items, sideNav.BottomItems);
    }

    [AvaloniaFact]
    public void SideNav_MainSelectedItem_CanBeSet()
    {
        var sideNav = new SideNav();
        var item = new MenuItem("home", "Home", IconKind.Home, "/home");

        sideNav.MainSelectedItem = item;

        Assert.Equal(item, sideNav.MainSelectedItem);
    }

    [AvaloniaFact]
    public void SideNav_BottomSelectedItem_CanBeSet()
    {
        var sideNav = new SideNav();
        var item = new MenuItem("settings", "Settings", IconKind.Settings, "/settings");

        sideNav.BottomSelectedItem = item;

        Assert.Equal(item, sideNav.BottomSelectedItem);
    }

    [AvaloniaFact]
    public void SideNav_IsCollapsed_CanBeToggled()
    {
        var sideNav = new SideNav();

        Assert.False(sideNav.IsCollapsed);
        sideNav.IsCollapsed = true;
        Assert.True(sideNav.IsCollapsed);
        sideNav.IsCollapsed = false;
        Assert.False(sideNav.IsCollapsed);
    }

    [AvaloniaFact]
    public void SideNav_MainAndBottomItems_AreIndependent()
    {
        var sideNav = new SideNav();
        
        var mainItems = new List<MenuItem>
        {
            new MenuItem("main1", "Main 1", IconKind.Home, "/main1")
        };
        
        var bottomItems = new List<MenuItem>
        {
            new MenuItem("bottom1", "Bottom 1", IconKind.Settings, "/bottom1")
        };

        sideNav.MainItems = mainItems;
        sideNav.BottomItems = bottomItems;

        Assert.NotEqual(sideNav.MainItems, sideNav.BottomItems);
        Assert.Single((System.Collections.IEnumerable)sideNav.MainItems!);
        Assert.Single((System.Collections.IEnumerable)sideNav.BottomItems!);
    }

    [AvaloniaFact]
    public void SideNav_SelectionProperties_AreIndependent()
    {
        var sideNav = new SideNav();
        var mainItem = new MenuItem("main", "Main", IconKind.Home, "/main");
        var bottomItem = new MenuItem("bottom", "Bottom", IconKind.Settings, "/bottom");

        sideNav.MainSelectedItem = mainItem;
        sideNav.BottomSelectedItem = bottomItem;

        Assert.NotEqual(sideNav.MainSelectedItem, sideNav.BottomSelectedItem);
        Assert.Equal(mainItem, sideNav.MainSelectedItem);
        Assert.Equal(bottomItem, sideNav.BottomSelectedItem);
    }
}

