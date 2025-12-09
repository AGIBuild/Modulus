using CommunityToolkit.Mvvm.Messaging;
using Modulus.UI.Abstractions;
using Modulus.UI.Abstractions.Messages;
using NSubstitute;

namespace Modulus.Hosts.Tests;

/// <summary>
/// Tests for ShellViewModel message handling for menu items.
/// These tests verify the behavior of MenuItemsRemovedMessage handling
/// which is critical for cleaning up menus when modules are unloaded.
/// </summary>
public class ShellViewModelTests : IDisposable
{
    private readonly IMenuRegistry _menuRegistry;
    private readonly TestableShellViewModel _viewModel;

    public ShellViewModelTests()
    {
        _menuRegistry = Substitute.For<IMenuRegistry>();
        _menuRegistry.GetItems(MenuLocation.Main).Returns(new List<MenuItem>());
        _menuRegistry.GetItems(MenuLocation.Bottom).Returns(new List<MenuItem>());
        
        _viewModel = new TestableShellViewModel(_menuRegistry);
    }

    public void Dispose()
    {
        WeakReferenceMessenger.Default.UnregisterAll(_viewModel);
    }

    [Fact]
    public void Receive_MenuItemsRemovedMessage_RemovesMenuItemsFromMainMenu()
    {
        // Arrange
        const string moduleId = "test-module";
        var menuItem = new MenuItem("menu1", "Test Menu", IconKind.Home, "/test", MenuLocation.Main)
        {
            ModuleId = moduleId
        };
        _viewModel.MainMenuItems.Add(menuItem);
        _viewModel.MainMenuItems.Add(new MenuItem("other", "Other", IconKind.Home, "/other", MenuLocation.Main));

        // Act
        _viewModel.Receive(new MenuItemsRemovedMessage(moduleId));

        // Assert
        Assert.Single(_viewModel.MainMenuItems);
        Assert.DoesNotContain(_viewModel.MainMenuItems, m => m.ModuleId == moduleId);
    }

    [Fact]
    public void Receive_MenuItemsRemovedMessage_RemovesMenuItemsFromBottomMenu()
    {
        // Arrange
        const string moduleId = "test-module";
        var menuItem = new MenuItem("menu1", "Settings", IconKind.Settings, "/settings", MenuLocation.Bottom)
        {
            ModuleId = moduleId
        };
        _viewModel.BottomMenuItems.Add(menuItem);
        _viewModel.BottomMenuItems.Add(new MenuItem("other", "Other", IconKind.Settings, "/other", MenuLocation.Bottom));

        // Act
        _viewModel.Receive(new MenuItemsRemovedMessage(moduleId));

        // Assert
        Assert.Single(_viewModel.BottomMenuItems);
        Assert.DoesNotContain(_viewModel.BottomMenuItems, m => m.ModuleId == moduleId);
    }

    [Fact]
    public void Receive_MenuItemsRemovedMessage_IgnoresUnrelatedModules()
    {
        // Arrange
        const string moduleId1 = "module-1";
        const string moduleId2 = "module-2";
        
        var menu1 = new MenuItem("menu1", "Menu 1", IconKind.Home, "/test1", MenuLocation.Main)
        {
            ModuleId = moduleId1
        };
        var menu2 = new MenuItem("menu2", "Menu 2", IconKind.Home, "/test2", MenuLocation.Main)
        {
            ModuleId = moduleId2
        };
        _viewModel.MainMenuItems.Add(menu1);
        _viewModel.MainMenuItems.Add(menu2);

        // Act - Remove only module-1's menus
        _viewModel.Receive(new MenuItemsRemovedMessage(moduleId1));

        // Assert - module-2's menu should remain
        Assert.Single(_viewModel.MainMenuItems);
        Assert.Contains(_viewModel.MainMenuItems, m => m.ModuleId == moduleId2);
        Assert.DoesNotContain(_viewModel.MainMenuItems, m => m.ModuleId == moduleId1);
    }

    [Fact]
    public void Receive_MenuItemsRemovedMessage_RemovesMultipleItemsFromSameModule()
    {
        // Arrange
        const string moduleId = "multi-menu-module";
        
        _viewModel.MainMenuItems.Add(new MenuItem("m1", "Menu 1", IconKind.Home, "/1") { ModuleId = moduleId });
        _viewModel.MainMenuItems.Add(new MenuItem("m2", "Menu 2", IconKind.Home, "/2") { ModuleId = moduleId });
        _viewModel.MainMenuItems.Add(new MenuItem("m3", "Menu 3", IconKind.Home, "/3") { ModuleId = moduleId });
        _viewModel.MainMenuItems.Add(new MenuItem("other", "Other", IconKind.Home, "/other") { ModuleId = "other-module" });

        // Act
        _viewModel.Receive(new MenuItemsRemovedMessage(moduleId));

        // Assert
        Assert.Single(_viewModel.MainMenuItems);
        Assert.Equal("other-module", _viewModel.MainMenuItems.First().ModuleId);
    }

    [Fact]
    public void Receive_MenuItemsRemovedMessage_HandlesEmptyCollections()
    {
        // Arrange - collections are empty by default
        Assert.Empty(_viewModel.MainMenuItems);
        Assert.Empty(_viewModel.BottomMenuItems);

        // Act - should not throw
        var exception = Record.Exception(() => 
            _viewModel.Receive(new MenuItemsRemovedMessage("non-existent")));

        // Assert
        Assert.Null(exception);
        Assert.Empty(_viewModel.MainMenuItems);
        Assert.Empty(_viewModel.BottomMenuItems);
    }

    [Fact]
    public void Receive_MenuItemsRemovedMessage_RemovesFromBothMenusSimultaneously()
    {
        // Arrange
        const string moduleId = "test-module";
        
        _viewModel.MainMenuItems.Add(new MenuItem("main1", "Main", IconKind.Home, "/main") { ModuleId = moduleId });
        _viewModel.BottomMenuItems.Add(new MenuItem("bottom1", "Bottom", IconKind.Settings, "/bottom") { ModuleId = moduleId });

        // Act
        _viewModel.Receive(new MenuItemsRemovedMessage(moduleId));

        // Assert
        Assert.Empty(_viewModel.MainMenuItems);
        Assert.Empty(_viewModel.BottomMenuItems);
    }
}

/// <summary>
/// Testable ShellViewModel that mimics the behavior of both Blazor and Avalonia ShellViewModels
/// for message handling. This isolates the menu removal logic for unit testing.
/// </summary>
internal class TestableShellViewModel : IRecipient<MenuItemsRemovedMessage>
{
    private readonly IMenuRegistry _menuRegistry;

    public System.Collections.ObjectModel.ObservableCollection<MenuItem> MainMenuItems { get; } = new();
    public System.Collections.ObjectModel.ObservableCollection<MenuItem> BottomMenuItems { get; } = new();

    public TestableShellViewModel(IMenuRegistry menuRegistry)
    {
        _menuRegistry = menuRegistry;
        WeakReferenceMessenger.Default.Register(this);
    }

    /// <summary>
    /// Handle incremental menu removal - mirrors the implementation in ShellViewModel.
    /// </summary>
    public void Receive(MenuItemsRemovedMessage message)
    {
        var mainToRemove = MainMenuItems.Where(m => m.ModuleId == message.ModuleId).ToList();
        foreach (var item in mainToRemove)
        {
            MainMenuItems.Remove(item);
        }
        
        var bottomToRemove = BottomMenuItems.Where(m => m.ModuleId == message.ModuleId).ToList();
        foreach (var item in bottomToRemove)
        {
            BottomMenuItems.Remove(item);
        }
    }
}

