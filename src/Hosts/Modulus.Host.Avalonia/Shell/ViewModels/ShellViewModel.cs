using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using Modulus.Host.Avalonia.Services;
using Modulus.UI.Abstractions;
using Modulus.UI.Abstractions.Messages;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Modulus.Host.Avalonia.Shell.ViewModels;

public partial class ShellViewModel : ViewModelBase, 
    IRecipient<MenuRefreshMessage>,
    IRecipient<MenuItemsAddedMessage>,
    IRecipient<MenuItemsRemovedMessage>
{
    private readonly IMenuRegistry _menuRegistry;
    private readonly INavigationService _navigationService;
    private readonly AvaloniaNavigationService? _avaloniaNavService;
    private readonly ILogger<ShellViewModel> _logger;
    private bool _isNavigating;

    public ObservableCollection<MenuItem> MainMenuItems { get; } = new();
    public ObservableCollection<MenuItem> BottomMenuItems { get; } = new();

    [ObservableProperty]
    private object? _currentView;

    [ObservableProperty]
    private string _currentTitle = "Modulus";

    [ObservableProperty]
    private MenuItem? _selectedMainMenuItem;

    [ObservableProperty]
    private MenuItem? _selectedBottomMenuItem;

    /// <summary>
    /// Whether the navigation panel is collapsed (icon-only mode).
    /// </summary>
    [ObservableProperty]
    private bool _isNavCollapsed;

    /// <summary>
    /// Inverse of IsNavCollapsed for binding to SplitView.IsPaneOpen.
    /// </summary>
    public bool IsPaneOpen => !IsNavCollapsed;

    public ShellViewModel(
        IMenuRegistry menuRegistry,
        INavigationService navigationService,
        ILogger<ShellViewModel> logger)
    {
        _menuRegistry = menuRegistry;
        _navigationService = navigationService;
        _logger = logger;
        
        // Get concrete implementation for callbacks
        _avaloniaNavService = (navigationService as AvaloniaNavigationService)!;
        if (_avaloniaNavService != null)
        {
            _avaloniaNavService.OnViewChanged = OnNavigationViewChanged;
        }

        // Subscribe to menu messages
        WeakReferenceMessenger.Default.RegisterAll(this);

        RefreshMenu();
    }
    
    /// <summary>
    /// Handle full menu refresh message - reload all menus from registry.
    /// </summary>
    public void Receive(MenuRefreshMessage message)
    {
        Dispatcher.UIThread.Post(RefreshMenu);
    }
    
    /// <summary>
    /// Handle incremental menu addition - add items without losing selection.
    /// </summary>
    public void Receive(MenuItemsAddedMessage message)
    {
        Dispatcher.UIThread.Post(() =>
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
        });
    }
    
    /// <summary>
    /// Handle incremental menu removal - update registry and UI in one place.
    /// </summary>
    public void Receive(MenuItemsRemovedMessage message)
    {
        // Unregister from MenuRegistry (source of truth)
        _menuRegistry.UnregisterModuleItems(message.ModuleId);
        
        // Update UI
        Dispatcher.UIThread.Post(() =>
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
        });
    }

    partial void OnIsNavCollapsedChanged(bool value)
    {
        OnPropertyChanged(nameof(IsPaneOpen));
    }

    [RelayCommand]
    private void ToggleNavCollapse()
    {
        IsNavCollapsed = !IsNavCollapsed;
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

    partial void OnSelectedMainMenuItemChanged(MenuItem? value)
    {
        if (value != null && !_isNavigating)
        {
            // Clear bottom selection
            _isNavigating = true;
            SelectedBottomMenuItem = null;
            _isNavigating = false;

            _ = NavigateToMenuItemAsync(value);
        }
    }

    partial void OnSelectedBottomMenuItemChanged(MenuItem? value)
    {
        if (value != null && !_isNavigating)
        {
            // Clear main selection
            _isNavigating = true;
            SelectedMainMenuItem = null;
            _isNavigating = false;

            _ = NavigateToMenuItemAsync(value);
        }
    }

    private async Task NavigateToMenuItemAsync(MenuItem item)
    {
        // Skip if disabled
        if (!item.IsEnabled)
        {
            return;
        }

        // Skip groups without navigation target
        if (string.IsNullOrEmpty(item.NavigationKey))
        {
            // Toggle expansion for groups
            item.IsExpanded = !item.IsExpanded;
            return;
        }

        // Use stable menu id as navigation key; resolve targets via registry/index in navigation service.
        var result = await _navigationService.NavigateToAsync(item.Id);
        if (!result)
        {
            _logger.LogWarning("Navigation failed or blocked: MenuId={MenuId}", item.Id);
        }
    }

    private void OnNavigationViewChanged(object? view, string title)
    {
        CurrentView = view;
        CurrentTitle = title;
    }

    /// <summary>
    /// Navigate to a specific ViewModel type directly.
    /// </summary>
    public void NavigateTo<TViewModel>() where TViewModel : class
    {
        var vmName = typeof(TViewModel).FullName;
        
        _isNavigating = true;
        try
        {
            _ = _navigationService.NavigateToAsync<TViewModel>();

            // Find and select corresponding menu item
            var mainItem = FindMenuItemByTarget(MainMenuItems, vmName);
            var bottomItem = FindMenuItemByTarget(BottomMenuItems, vmName);
            
            if (mainItem == null && bottomItem == null && !string.IsNullOrWhiteSpace(vmName))
            {
                _logger.LogWarning("NavigateTo: no menu item found for NavigationKey={NavKey}", vmName);
            }

            if (mainItem != null)
            {
                ExpandParents(MainMenuItems, mainItem);
                SelectedMainMenuItem = mainItem;
                SelectedBottomMenuItem = null;
            }
            else if (bottomItem != null)
            {
                ExpandParents(BottomMenuItems, bottomItem);
                SelectedBottomMenuItem = bottomItem;
                SelectedMainMenuItem = null;
            }
        }
        finally
        {
            _isNavigating = false;
        }
    }

    /// <summary>
    /// Navigate to a view by its navigation key (route string).
    /// </summary>
    public void NavigateToRoute(string navigationKey)
    {
        _isNavigating = true;
        try
        {
            // For Avalonia host, navigationKey is a stable menu id.
            _ = _navigationService.NavigateToAsync(navigationKey);

            // Find and select corresponding menu item
            var mainItem = FindMenuItemById(MainMenuItems, navigationKey);
            var bottomItem = FindMenuItemById(BottomMenuItems, navigationKey);
            
            if (mainItem == null && bottomItem == null && !string.IsNullOrWhiteSpace(navigationKey))
            {
                _logger.LogWarning("NavigateToRoute: no menu item found for NavigationKey={NavKey}", navigationKey);
            }

            if (mainItem != null)
            {
                ExpandParents(MainMenuItems, mainItem);
                SelectedMainMenuItem = mainItem;
                SelectedBottomMenuItem = null;
            }
            else if (bottomItem != null)
            {
                ExpandParents(BottomMenuItems, bottomItem);
                SelectedBottomMenuItem = bottomItem;
                SelectedMainMenuItem = null;
            }
        }
        finally
        {
            _isNavigating = false;
        }
    }

    private static MenuItem? FindMenuItemByTarget(ObservableCollection<MenuItem> roots, string? target)
    {
        if (string.IsNullOrWhiteSpace(target)) return null;

        foreach (var root in roots)
        {
            if (string.Equals(root.NavigationKey, target, StringComparison.Ordinal))
                return root;

            if (root.Children == null) continue;
            foreach (var child in root.Children)
            {
                if (string.Equals(child.NavigationKey, target, StringComparison.Ordinal))
                    return child;
            }
        }

        return null;
    }

    private static MenuItem? FindMenuItemById(ObservableCollection<MenuItem> roots, string? id)
    {
        if (string.IsNullOrWhiteSpace(id)) return null;

        foreach (var root in roots)
        {
            if (string.Equals(root.Id, id, StringComparison.OrdinalIgnoreCase))
                return root;

            if (root.Children == null) continue;
            foreach (var child in root.Children)
            {
                if (string.Equals(child.Id, id, StringComparison.OrdinalIgnoreCase))
                    return child;
            }
        }

        return null;
    }

    private static void ExpandParents(ObservableCollection<MenuItem> roots, MenuItem selected)
    {
        foreach (var root in roots)
        {
            if (root.Children == null) continue;
            if (root.Children.Any(c => c.Id == selected.Id))
            {
                root.IsExpanded = true;
                return;
            }
        }
    }

    /// <summary>
    /// Select a menu item programmatically (updates selection state).
    /// </summary>
    public void SelectMenuItem(MenuItem item)
    {
        _isNavigating = true;
        try
        {
            if (item.Location == MenuLocation.Main)
            {
                SelectedMainMenuItem = item;
                SelectedBottomMenuItem = null;
            }
            else
            {
                SelectedBottomMenuItem = item;
                SelectedMainMenuItem = null;
            }
        }
        finally
        {
            _isNavigating = false;
        }
    }
}
