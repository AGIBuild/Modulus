using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Modulus.UI.Abstractions;
using Modulus.UI.Abstractions.Messages;
using System.Collections.ObjectModel;
using System.Linq;
using UiMenuItem = Modulus.UI.Abstractions.MenuItem;

namespace Modulus.Host.Blazor.Shell.ViewModels;

public partial class ShellViewModel : ObservableObject, 
    IRecipient<MenuRefreshMessage>,
    IRecipient<MenuItemsAddedMessage>,
    IRecipient<MenuItemsRemovedMessage>
{
    private readonly IMenuRegistry _menuRegistry;

    public ObservableCollection<UiMenuItem> MainMenuItems { get; } = new();
    public ObservableCollection<UiMenuItem> BottomMenuItems { get; } = new();

    [ObservableProperty]
    private string _currentTitle = "Modulus";

    [ObservableProperty]
    private UiMenuItem? _selectedMenuItem;

    public ShellViewModel(IMenuRegistry menuRegistry)
    {
        _menuRegistry = menuRegistry;
        
        // Subscribe to menu messages
        WeakReferenceMessenger.Default.RegisterAll(this);
        
        RefreshMenu();
    }
    
    /// <summary>
    /// Handle full menu refresh message - reload all menus from registry.
    /// </summary>
    public void Receive(MenuRefreshMessage message)
    {
        RefreshMenu();
    }
    
    /// <summary>
    /// Handle incremental menu addition - add items without losing selection.
    /// </summary>
    public void Receive(MenuItemsAddedMessage message)
    {
        foreach (var item in message.Items)
        {
            var collection = item.Location == MenuLocation.Main ? MainMenuItems : BottomMenuItems;
            
            // Avoid duplicates
            if (collection.All(m => m.Id != item.Id))
            {
                // Insert in order
                var index = collection.Count(m => m.Order < item.Order);
                collection.Insert(index, item);
            }
        }
    }
    
    /// <summary>
    /// Handle incremental menu removal - remove items without losing selection.
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

    public void RefreshMenu()
    {
        MainMenuItems.Clear();
        foreach (var item in _menuRegistry.GetItems(MenuLocation.Main))
        {
            MainMenuItems.Add(item);
        }

        BottomMenuItems.Clear();
        foreach (var item in _menuRegistry.GetItems(MenuLocation.Bottom))
        {
            BottomMenuItems.Add(item);
        }
    }

    public void SelectMenuItem(UiMenuItem item)
    {
        SelectedMenuItem = item;
        CurrentTitle = item.DisplayName;
    }
}
