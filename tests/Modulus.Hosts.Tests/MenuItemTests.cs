using Modulus.UI.Abstractions;

namespace Modulus.Hosts.Tests;

/// <summary>
/// Tests for MenuItem enhanced properties.
/// </summary>
public class MenuItemTests
{
    [Fact]
    public void MenuItem_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var item = new MenuItem("id", "Display", "icon", "/route");

        // Assert
        Assert.Equal("id", item.Id);
        Assert.Equal("Display", item.DisplayName);
        Assert.Equal("icon", item.Icon);
        Assert.Equal("/route", item.NavigationKey);
        Assert.Equal(MenuLocation.Main, item.Location);
        Assert.Equal(0, item.Order);
        Assert.True(item.IsEnabled);
        Assert.Null(item.BadgeCount);
        Assert.Null(item.BadgeColor);
        Assert.Equal(PageInstanceMode.Default, item.InstanceMode);
        Assert.Null(item.Children);
        Assert.Null(item.ContextActions);
        Assert.False(item.IsExpanded);
    }

    [Fact]
    public void MenuItem_WithLocation_SetsCorrectly()
    {
        // Arrange & Act
        var item = new MenuItem("id", "Display", "icon", "/route", MenuLocation.Bottom, 10);

        // Assert
        Assert.Equal(MenuLocation.Bottom, item.Location);
        Assert.Equal(10, item.Order);
    }

    [Fact]
    public void MenuItem_IsEnabled_CanBeModified()
    {
        // Arrange
        var item = new MenuItem("id", "Display", "icon", "/route");

        // Act
        item.IsEnabled = false;

        // Assert
        Assert.False(item.IsEnabled);
    }

    [Fact]
    public void MenuItem_BadgeCount_CanBeSet()
    {
        // Arrange
        var item = new MenuItem("id", "Display", "icon", "/route");

        // Act
        item.BadgeCount = 5;
        item.BadgeColor = "error";

        // Assert
        Assert.Equal(5, item.BadgeCount);
        Assert.Equal("error", item.BadgeColor);
    }

    [Fact]
    public void MenuItem_InstanceMode_CanBeModified()
    {
        // Arrange
        var item = new MenuItem("id", "Display", "icon", "/route");

        // Act
        item.InstanceMode = PageInstanceMode.Transient;

        // Assert
        Assert.Equal(PageInstanceMode.Transient, item.InstanceMode);
    }

    [Fact]
    public void MenuItem_Children_CanBeAssigned()
    {
        // Arrange
        var parent = new MenuItem("parent", "Parent", "folder", "");
        var child1 = new MenuItem("child1", "Child 1", "file", "/child1");
        var child2 = new MenuItem("child2", "Child 2", "file", "/child2");

        // Act
        parent.Children = new List<MenuItem> { child1, child2 };

        // Assert
        Assert.NotNull(parent.Children);
        Assert.Equal(2, parent.Children.Count);
        Assert.Contains(child1, parent.Children);
        Assert.Contains(child2, parent.Children);
    }

    [Fact]
    public void MenuItem_IsExpanded_CanBeToggled()
    {
        // Arrange
        var item = new MenuItem("id", "Display", "icon", "/route");
        item.Children = new List<MenuItem> { new MenuItem("child", "Child", "icon", "/child") };

        // Act & Assert
        Assert.False(item.IsExpanded);
        item.IsExpanded = true;
        Assert.True(item.IsExpanded);
        item.IsExpanded = false;
        Assert.False(item.IsExpanded);
    }

    [Fact]
    public void MenuItem_ContextActions_CanBeAssigned()
    {
        // Arrange
        var item = new MenuItem("id", "Display", "icon", "/route");
        var actionExecuted = false;

        var actions = new List<MenuAction>
        {
            new MenuAction
            {
                Label = "Edit",
                Icon = "edit",
                Execute = _ => actionExecuted = true
            },
            new MenuAction
            {
                Label = "Delete",
                Icon = "delete",
                Execute = _ => { }
            }
        };

        // Act
        item.ContextActions = actions;
        item.ContextActions[0].Execute(item);

        // Assert
        Assert.NotNull(item.ContextActions);
        Assert.Equal(2, item.ContextActions.Count);
        Assert.True(actionExecuted);
    }

    [Fact]
    public void MenuItem_CreateGroup_CreatesGroupWithChildren()
    {
        // Arrange
        var children = new List<MenuItem>
        {
            new MenuItem("c1", "Child 1", "icon", "/c1"),
            new MenuItem("c2", "Child 2", "icon", "/c2")
        };

        // Act
        var group = MenuItem.CreateGroup("grp", "Group", "folder", children, MenuLocation.Main, 5);

        // Assert
        Assert.Equal("grp", group.Id);
        Assert.Equal("Group", group.DisplayName);
        Assert.Equal("folder", group.Icon);
        Assert.Equal(string.Empty, group.NavigationKey);
        Assert.Equal(MenuLocation.Main, group.Location);
        Assert.Equal(5, group.Order);
        Assert.NotNull(group.Children);
        Assert.Equal(2, group.Children.Count);
    }
}

/// <summary>
/// Tests for PageInstanceMode enum.
/// </summary>
public class PageInstanceModeTests
{
    [Fact]
    public void PageInstanceMode_HasExpectedValues()
    {
        // Assert
        Assert.Equal(0, (int)PageInstanceMode.Default);
        Assert.Equal(1, (int)PageInstanceMode.Singleton);
        Assert.Equal(2, (int)PageInstanceMode.Transient);
    }
}

/// <summary>
/// Tests for NavigationOptions class.
/// </summary>
public class NavigationOptionsTests
{
    [Fact]
    public void NavigationOptions_DefaultValues()
    {
        // Arrange & Act
        var options = new NavigationOptions();

        // Assert
        Assert.False(options.ForceNewInstance);
        Assert.Null(options.Parameters);
    }

    [Fact]
    public void NavigationOptions_WithParameters()
    {
        // Arrange & Act
        var options = new NavigationOptions
        {
            ForceNewInstance = true,
            Parameters = new Dictionary<string, object>
            {
                { "id", 123 },
                { "name", "test" }
            }
        };

        // Assert
        Assert.True(options.ForceNewInstance);
        Assert.NotNull(options.Parameters);
        Assert.Equal(123, options.Parameters["id"]);
        Assert.Equal("test", options.Parameters["name"]);
    }
}

/// <summary>
/// Tests for NavigationContext class.
/// </summary>
public class NavigationContextTests
{
    [Fact]
    public void NavigationContext_Properties()
    {
        // Arrange & Act
        var context = new NavigationContext
        {
            FromKey = "/from",
            ToKey = "/to",
            Options = new NavigationOptions { ForceNewInstance = true }
        };

        // Assert
        Assert.Equal("/from", context.FromKey);
        Assert.Equal("/to", context.ToKey);
        Assert.True(context.Options.ForceNewInstance);
    }

    [Fact]
    public void NavigationContext_DefaultOptions()
    {
        // Arrange & Act
        var context = new NavigationContext { ToKey = "/target" };

        // Assert
        Assert.Null(context.FromKey);
        Assert.Equal("/target", context.ToKey);
        Assert.NotNull(context.Options);
        Assert.False(context.Options.ForceNewInstance);
    }
}

