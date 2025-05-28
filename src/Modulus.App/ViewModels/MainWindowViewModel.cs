using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Configuration;
using Modulus.App.Controls.ViewModels;
using Modulus.App.Services;
using Modulus.Plugin.Abstractions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Modulus.App.ViewModels
{
    public partial class MainWindowViewModel : ObservableObject
    {
        private readonly IPluginManager _pluginManager;
        private readonly INavigationService _navigationService;
        private readonly IConfiguration _configuration;
        private readonly NavigationPluginService _navigationPluginService;

        [ObservableProperty]
        private NavigationViewModel _navigation;

        public MainWindowViewModel(
            IPluginManager pluginManager,
            INavigationService navigationService,
            NavigationPluginService navigationPluginService,
            IConfiguration configuration)
        {
            _pluginManager = pluginManager ?? throw new ArgumentNullException(nameof(pluginManager));
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
            _navigationPluginService = navigationPluginService ?? throw new ArgumentNullException(nameof(navigationPluginService));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

            Navigation = new NavigationViewModel(_navigationService);
            _navigationService.SetNavigationViewModel(Navigation);
            _navigationPluginService.SetMainViewModel(this);

            InitializeNavigationMenu();
            Task.Run(async () => await LoadPluginsAsync());
            
            _navigationService.NavigateTo("DashboardView");
        }

        private void InitializeNavigationMenu()
        {
            Navigation.AddNavigationItem("仪表盘", "home_regular", "DashboardView");
            Navigation.AddNavigationItem("插件管理", "puzzle_regular", "PluginManagerView");
            Navigation.AddNavigationItem("设置", "settings_regular", "SettingsView", "footer");

            var notificationsItem = Navigation.AddNavigationItem("通知", "info_regular", "NotificationsView");
            notificationsItem.SetBadge(3);
        }

        private async Task LoadPluginsAsync()
        {
            try
            {
                var pluginsPath = _configuration.GetValue<string>("PluginsPath") ??
                                 Path.Combine(AppContext.BaseDirectory, "plugins");

                if (!Directory.Exists(pluginsPath))
                {
                    Directory.CreateDirectory(pluginsPath);
                }

                if (_pluginManager == null)
                {
                    System.Diagnostics.Debug.WriteLine("PluginManager is null in LoadPluginsAsync.");
                    return;
                }

                IEnumerable<IPlugin> loadedPlugins = await _pluginManager.LoadPluginsAsync(pluginsPath);

                // Register plugin views and add them to navigation
                foreach (var plugin in loadedPlugins)
                {
                    var meta = plugin.GetMetadata();
                    if (meta == null)
                    {
                        System.Diagnostics.Debug.WriteLine("Plugin metadata is null for a loaded plugin.");
                        continue;
                    }

                    // Register the plugin's main view for navigation
                    var containerVm = new PluginContainerViewModel
                    {
                        PluginName = meta.Name,
                        PluginView = plugin.GetMainView()
                    };
                    var viewName = $"Plugin_{meta.Name}";
                    _navigationService.RegisterViewModel(viewName, () => containerVm);
                }
                
                // Add loaded plugins to the navigation panel
                _navigationPluginService.AddPluginNavigationItems(loadedPlugins);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading plugins: {ex.Message}");
                System.Diagnostics.Debug.WriteLine(ex.StackTrace);
                if (Navigation != null)
                {
                     Navigation.StatusMessage = $"Error loading plugins: {ex.Message}";
                }
            }
        }

        [RelayCommand]
        private async Task ReloadPluginsAsync()
        {
            System.Diagnostics.Debug.WriteLine("ReloadPluginsAsync command executed.");

            if (_pluginManager == null)
            {
                System.Diagnostics.Debug.WriteLine("PluginManager is null, cannot reload plugins.");
                return;
            }

            // Unload all currently loaded plugins
            foreach (var plugin in _pluginManager.LoadedPlugins)
            {
                if (plugin.GetMetadata() != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Unloading plugin: {plugin.GetMetadata().Name}");
                    await _pluginManager.UnloadPluginAsync(plugin.GetMetadata().Name);
                }
            }
            
            // Call the main LoadPluginsAsync to reload all plugins and rebuild everything
            await LoadPluginsAsync();
            System.Diagnostics.Debug.WriteLine("Finished reloading plugins.");
        }
    }
}

