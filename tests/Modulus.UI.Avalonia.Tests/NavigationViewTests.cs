using Avalonia.Headless.XUnit;
using Modulus.UI.Abstractions;
using Modulus.UI.Avalonia.Controls;

namespace Modulus.UI.Avalonia.Tests;

/// <summary>
/// Tests for NavigationView control.
/// </summary>
public class NavigationViewTests
{
    [AvaloniaFact]
    public void NavigationView_DefaultProperties()
    {
        var nav = new NavigationView();

        Assert.Null(nav.SelectedItem);
        Assert.False(nav.IsCollapsed);
    }

    [AvaloniaFact]
    public void NavigationView_SelectedItem_CanBeSet()
    {
        var nav = new NavigationView();
        var item = new MenuItem("test", "Test", IconKind.Home, "/test");

        nav.SelectedItem = item;

        Assert.Equal(item, nav.SelectedItem);
    }

    [AvaloniaFact]
    public void NavigationView_IsCollapsed_CanBeToggled()
    {
        var nav = new NavigationView();

        Assert.False(nav.IsCollapsed);
        nav.IsCollapsed = true;
        Assert.True(nav.IsCollapsed);
        nav.IsCollapsed = false;
        Assert.False(nav.IsCollapsed);
    }

    [AvaloniaFact]
    public void NavigationView_SelectionChanged_EventRaised()
    {
        var nav = new NavigationView();
        var item = new MenuItem("test", "Test", IconKind.Home, "/test");
        MenuItem? receivedItem = null;

        nav.SelectionChanged += (sender, e) => receivedItem = e;
        nav.SelectedItem = item;

        Assert.Equal(item, receivedItem);
    }

    [AvaloniaFact]
    public void NavigationView_ItemInvoked_EventExists()
    {
        var nav = new NavigationView();
        
        // Verify event can be subscribed
        nav.ItemInvoked += (sender, item) => { };
        
        // No exception means success
        Assert.True(true);
    }

    [AvaloniaFact]
    public void NavigationView_ItemsSource_CanBeSet()
    {
        var nav = new NavigationView();
        var items = new List<MenuItem>
        {
            new MenuItem("item1", "Item 1", IconKind.Home, "/item1"),
            new MenuItem("item2", "Item 2", IconKind.Settings, "/item2")
        };

        nav.ItemsSource = items;

        Assert.Equal(items, nav.ItemsSource);
    }

    [AvaloniaFact]
    public void NavigationView_GroupClick_TogglesExpansion_AndDoesNotSelect()
    {
        var nav = new NavigationView();
        var group = MenuItem.CreateGroup(
            "parent",
            "Parent",
            IconKind.Home,
            new List<MenuItem> { new MenuItem("child", "Child", IconKind.Grid, "/child") });

        var navItem = new NavigationViewItem { Item = group };

        // Invoke internal click handler via reflection (headless test).
        var method = typeof(NavigationView).GetMethod("OnItemClicked", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        Assert.NotNull(method);

        Assert.False(group.IsExpanded);
        Assert.Null(nav.SelectedItem);

        method!.Invoke(nav, new object[] { navItem, group });

        Assert.True(group.IsExpanded);
        Assert.Null(nav.SelectedItem);
    }
}

/// <summary>
/// Tests for NavigationViewItem control.
/// </summary>
public class NavigationViewItemTests
{
    [AvaloniaFact]
    public void NavigationViewItem_DefaultProperties()
    {
        var navItem = new NavigationViewItem();

        Assert.Null(navItem.Item);
        Assert.False(navItem.IsSelected);
        Assert.False(navItem.IsExpanded);
        Assert.False(navItem.IsCollapsed);
        Assert.Equal(0, navItem.Depth);
    }

    [AvaloniaFact]
    public void NavigationViewItem_Item_CanBeSet()
    {
        var navItem = new NavigationViewItem();
        var menuItem = new MenuItem("test", "Test", IconKind.Home, "/test");

        navItem.Item = menuItem;

        Assert.Equal(menuItem, navItem.Item);
    }

    [AvaloniaFact]
    public void NavigationViewItem_IsSelected_CanBeToggled()
    {
        var navItem = new NavigationViewItem();

        Assert.False(navItem.IsSelected);
        navItem.IsSelected = true;
        Assert.True(navItem.IsSelected);
    }

    [AvaloniaFact]
    public void NavigationViewItem_IsExpanded_CanBeToggled()
    {
        var navItem = new NavigationViewItem();

        Assert.False(navItem.IsExpanded);
        navItem.IsExpanded = true;
        Assert.True(navItem.IsExpanded);
    }

    [AvaloniaFact]
    public void NavigationViewItem_IsCollapsed_CanBeToggled()
    {
        var navItem = new NavigationViewItem();

        Assert.False(navItem.IsCollapsed);
        navItem.IsCollapsed = true;
        Assert.True(navItem.IsCollapsed);
    }

    [AvaloniaFact]
    public void NavigationViewItem_Depth_CanBeSet()
    {
        var navItem = new NavigationViewItem();

        navItem.Depth = 2;

        Assert.Equal(2, navItem.Depth);
    }

    [AvaloniaFact]
    public void NavigationViewItem_HasBadgeConverter_IsNotNull()
    {
        Assert.NotNull(NavigationViewItem.HasBadgeConverter);
    }

    [AvaloniaFact]
    public void NavigationViewItem_IconToGeometryConverter_IsNotNull()
    {
        Assert.NotNull(NavigationViewItem.IconToGeometryConverter);
    }
}

