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
            _logger.LogDebug("Added Main menu: {Id}, DisplayName={Name}, NavigationKey={NavKey}",
                item.Id, item.DisplayName, item.NavigationKey ?? "null");
        }

        BottomMenuItems.Clear();
        foreach (var item in _menuRegistry.GetItems(MenuLocation.Bottom))
        {
            BottomMenuItems.Add(item);
            _logger.LogDebug("Added Bottom menu: {Id}, DisplayName={Name}, NavigationKey={NavKey}",
                item.Id, item.DisplayName, item.NavigationKey ?? "null");
        }
        
        _logger.LogInformation("Menu refreshed: {MainCount} main items, {BottomCount} bottom items",
            MainMenuItems.Count, BottomMenuItems.Count);
    }

    partial void OnSelectedMainMenuItemChanged(MenuItem? value)
    {
        _logger.LogInformation("OnSelectedMainMenuItemChanged: value={DisplayName}, NavigationKey={NavKey}, _isNavigating={IsNav}",
            value?.DisplayName ?? "null", value?.NavigationKey ?? "null", _isNavigating);
            
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
        _logger.LogInformation("NavigateToMenuItemAsync: {DisplayName}, IsEnabled={IsEnabled}, NavigationKey={NavKey}",
            item.DisplayName, item.IsEnabled, item.NavigationKey ?? "null");
            
        // Skip if disabled
        if (!item.IsEnabled)
        {
            _logger.LogInformation("Navigation skipped: item is disabled");
            return;
        }

        // Skip groups without navigation key
        if (string.IsNullOrEmpty(item.NavigationKey))
        {
            _logger.LogInformation("Navigation skipped: no NavigationKey, toggling expansion");
            // Toggle expansion for groups
            item.IsExpanded = !item.IsExpanded;
            return;
        }

        _logger.LogInformation("Calling NavigationService.NavigateToAsync({NavKey})", item.NavigationKey);
        var result = await _navigationService.NavigateToAsync(item.NavigationKey);
        _logger.LogInformation("NavigateToAsync result: {Result}", result);
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
        _logger.LogInformation("NavigateTo<{VMType}>: looking for NavigationKey={NavKey}", typeof(TViewModel).Name, vmName);
        
        _isNavigating = true;
        try
        {
            _ = _navigationService.NavigateToAsync<TViewModel>();

            // Find and select corresponding menu item
            var mainItem = MainMenuItems.FirstOrDefault(m => m.NavigationKey == vmName);
            var bottomItem = BottomMenuItems.FirstOrDefault(m => m.NavigationKey == vmName);
            
            _logger.LogInformation("NavigateTo: Found mainItem={MainFound}, bottomItem={BottomFound}",
                mainItem?.DisplayName ?? "null", bottomItem?.DisplayName ?? "null");

            if (mainItem != null)
            {
                SelectedMainMenuItem = mainItem;
                SelectedBottomMenuItem = null;
            }
            else if (bottomItem != null)
            {
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
        _logger.LogInformation("NavigateToRoute: {NavKey}", navigationKey);
        
        _isNavigating = true;
        try
        {
            _ = _navigationService.NavigateToAsync(navigationKey);

            // Find and select corresponding menu item
            var mainItem = MainMenuItems.FirstOrDefault(m => m.NavigationKey == navigationKey);
            var bottomItem = BottomMenuItems.FirstOrDefault(m => m.NavigationKey == navigationKey);
            
            _logger.LogInformation("NavigateToRoute: Found mainItem={MainFound}, bottomItem={BottomFound}",
                mainItem?.DisplayName ?? "null", bottomItem?.DisplayName ?? "null");

            if (mainItem != null)
            {
                SelectedMainMenuItem = mainItem;
                SelectedBottomMenuItem = null;
            }
            else if (bottomItem != null)
            {
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
