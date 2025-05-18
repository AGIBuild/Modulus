using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Modulus.App.Controls.ViewModels;
using Modulus.App.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Modulus.App.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly PluginManager _pluginManager;

    [ObservableProperty] private object? currentView;
    [ObservableProperty] private bool isDashboardActive;
    [ObservableProperty] private bool isPluginManagerActive;
    [ObservableProperty] private bool isNotificationsActive;
    [ObservableProperty] private bool isSettingsActive;
    [ObservableProperty] private bool isProfileActive;

    [ObservableProperty] private NavigationBarViewModel navigationBar;
    
    public MainViewModel()
    {
        navigationBar = new NavigationBarViewModel();

        // Create the navigation service and connect it to this view model
        var navigationService = new NavigationPluginService(navigationBar);
        navigationService.SetMainViewModel(this);

        // Create the plugin manager with the navigation service
        _pluginManager = new PluginManager(navigationBar, navigationService);

        InitializeNavigationMenus();
        ShowDashboard();

        // Auto-load plugins in background
        Task.Run(async () => await LoadPluginsAsync());
        
        // Demo notifications with badges
        SetupNotificationDemo();
    }
    
    /// <summary>
    /// Sets up a demo of notifications with badges
    /// </summary>
    private void SetupNotificationDemo()
    {
        // Find the notifications menu item and add a badge
        var notificationsItem = NavigationBar.BodyItems.FirstOrDefault(item => item.Tooltip == "Notifications");
        if (notificationsItem != null)
        {
            notificationsItem.SetBadge(3);
        }
        
        // Find the settings menu item and add a dot badge
        var settingsItem = NavigationBar.FooterItems.FirstOrDefault(item => item.Tooltip == "Settings");
        if (settingsItem != null)
        {
            settingsItem.SetDotBadge();
        }
    }
    
    /// <summary>
    /// Loads plugins from the default plugins directory.
    /// </summary>
    private async Task LoadPluginsAsync()
    {
        try
        {
            // In a real implementation, this would use a configuration setting
            const string pluginsPath = @"c:\FileStorage\Projects\Modulus\tools\modulus-plugin";

            // Load the simple plugin example for testing
            await _pluginManager.LoadPluginsAsync(pluginsPath);

            // For testing, manually add our SimplePluginExample
            _pluginManager.AddTestPlugins();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading plugins: {ex.Message}");
        }
    }

    /// <summary>
    /// Initialize the navigation menus with their respective icons, tooltips, and commands.
    /// </summary>
    private void InitializeNavigationMenus()
    {
        // Initialize body menu items
        NavigationBar.BodyItems.Add(new NavigationMenuItemViewModel
        {
            Icon = "\uE80F",
            Tooltip = "Dashboard",
            Command = ShowDashboardCommand,
            IsActive = true
        });

        NavigationBar.BodyItems.Add(new NavigationMenuItemViewModel
        {
            Icon = "\uE721",
            Tooltip = "Plugin Manager",
            Command = ShowPluginManagerCommand
        });

        NavigationBar.BodyItems.Add(new NavigationMenuItemViewModel
        {
            Icon = "\uE7E7",
            Tooltip = "Notifications",
            Command = ShowNotificationsCommand
        });

        // Initialize footer menu items
        NavigationBar.FooterItems.Add(new NavigationMenuItemViewModel
        {
            Icon = "\uE713",
            Tooltip = "Settings",
            Command = ShowSettingsCommand,
            Width = 48,
            Height = 48,
            CornerRadius = 10,
            FontSize = 22
        });

        NavigationBar.FooterItems.Add(new NavigationMenuItemViewModel
        {
            Icon = "\uE939",
            Tooltip = "Feedback",
            Command = new RelayCommand(ShowFeedback),
            Width = 48,
            Height = 48,
            CornerRadius = 10,
            FontSize = 22
        });

        NavigationBar.FooterItems.Add(new NavigationMenuItemViewModel
        {
            Icon = "\uE77B",
            Tooltip = "Profile",
            Command = ShowProfileCommand,
            Width = 48,
            Height = 48,
            CornerRadius = 10,
            FontSize = 22
        });
    }

    /// <summary>
    /// Updates the active state of navigation menu items based on the current view.
    /// </summary>
    private void UpdateNavigationMenuActiveState()
    {
        foreach (var item in NavigationBar.HeaderItems)
        {
            item.IsActive = false;
        }

        foreach (var item in NavigationBar.BodyItems)
        {
            if (item.Tooltip == "Dashboard") item.IsActive = IsDashboardActive;
            else if (item.Tooltip == "Plugin Manager") item.IsActive = IsPluginManagerActive;
            else if (item.Tooltip == "Notifications") item.IsActive = IsNotificationsActive;
            else item.IsActive = false;
        }

        foreach (var item in NavigationBar.FooterItems)
        {
            if (item.Tooltip == "Settings") item.IsActive = IsSettingsActive;
            else if (item.Tooltip == "Profile") item.IsActive = IsProfileActive;
            else item.IsActive = false;
        }
    }

    [RelayCommand]
    private void ShowDashboard()
    {
        SetActive("dashboard");
        CurrentView = new DashboardPlaceholderViewModel();
        UpdateNavigationMenuActiveState();
    }

    [RelayCommand]
    private void ShowPluginManager()
    {
        SetActive("plugin");
        CurrentView = new PluginManagerViewModel(_pluginManager);
        UpdateNavigationMenuActiveState();
    }

    [RelayCommand]
    private void ShowNotifications()
    {
        SetActive("notifications");
        CurrentView = new NotificationsPlaceholderViewModel();
        UpdateNavigationMenuActiveState();
        
        // Clear the notification badge when viewing notifications
        var notificationsItem = NavigationBar.BodyItems.FirstOrDefault(item => item.Tooltip == "Notifications");
        if (notificationsItem != null)
        {
            notificationsItem.ClearBadge();
        }
    }

    [RelayCommand]
    private void ShowSettings()
    {
        SetActive("settings");
        CurrentView = new SettingsPlaceholderViewModel();
        UpdateNavigationMenuActiveState();
        
        // Clear the settings badge when viewing settings
        var settingsItem = NavigationBar.FooterItems.FirstOrDefault(item => item.Tooltip == "Settings");
        if (settingsItem != null)
        {
            settingsItem.ClearBadge();
        }
    }

    [RelayCommand]
    private void ShowProfile()
    {
        SetActive("profile");
        CurrentView = new ProfilePlaceholderViewModel();
        UpdateNavigationMenuActiveState();
    }
    
    private void ShowFeedback()
    {
        // TODO: Implement feedback functionality
    }

    /// <summary>
    /// Sets the active state for the different views in the application.
    /// </summary>
    /// <param name="key">Key identifying the view to activate.</param>
    public void SetActive(string key)
    {
        IsDashboardActive = key == "dashboard";
        IsPluginManagerActive = key == "plugin";
        IsNotificationsActive = key == "notifications";
        IsSettingsActive = key == "settings";
        IsProfileActive = key == "profile";
    }
}

// 占位 ViewModel
public class DashboardPlaceholderViewModel { }
public class NotificationsPlaceholderViewModel { }
public class SettingsPlaceholderViewModel { }
public class ProfilePlaceholderViewModel { }
