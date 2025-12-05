using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Modulus.Host.Avalonia.Services;
using Modulus.UI.Abstractions;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Modulus.Host.Avalonia.Shell.ViewModels;

public partial class ShellViewModel : ViewModelBase
{
    private readonly IMenuRegistry _menuRegistry;
    private readonly INavigationService _navigationService;
    private readonly AvaloniaNavigationService? _avaloniaNavService;
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
        INavigationService navigationService)
    {
        _menuRegistry = menuRegistry;
        _navigationService = navigationService;
        
        // Get concrete implementation for callbacks
        _avaloniaNavService = (navigationService as AvaloniaNavigationService)!;
        if (_avaloniaNavService != null)
        {
            _avaloniaNavService.OnViewChanged = OnNavigationViewChanged;
        }

        RefreshMenu();
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

        // Skip groups without navigation key
        if (string.IsNullOrEmpty(item.NavigationKey))
        {
            // Toggle expansion for groups
            item.IsExpanded = !item.IsExpanded;
            return;
        }

        await _navigationService.NavigateToAsync(item.NavigationKey);
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
        _isNavigating = true;
        try
        {
            _ = _navigationService.NavigateToAsync<TViewModel>();

            // Find and select corresponding menu item
            var vmName = typeof(TViewModel).FullName;
            var mainItem = MainMenuItems.FirstOrDefault(m => m.NavigationKey == vmName);
            var bottomItem = BottomMenuItems.FirstOrDefault(m => m.NavigationKey == vmName);

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
