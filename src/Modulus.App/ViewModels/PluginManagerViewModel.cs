using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Modulus.App.Services;
using Modulus.Plugin.Abstractions;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Modulus.App.ViewModels
{
    /// <summary>
    /// ViewModel for the Plugin Manager view.
    /// </summary>
    public partial class PluginManagerViewModel : ObservableObject
    {
        private readonly PluginManager? _pluginManager;

        [ObservableProperty]
        private ObservableCollection<PluginViewModel> plugins = new();

        [ObservableProperty]
        private PluginViewModel? selectedPlugin;

        [ObservableProperty]
        private string searchText = string.Empty;

        [ObservableProperty]
        private ObservableCollection<PluginViewModel> recentlyUpdatedPlugins = new();

        [ObservableProperty]
        private bool isLoading;

        [ObservableProperty]
        private string statusMessage = string.Empty;

        public PluginManagerViewModel()
        {
            // Default constructor for design-time support
        }

        public PluginManagerViewModel(PluginManager pluginManager)
        {
            _pluginManager = pluginManager;
            RefreshPluginsList();
        }

        /// <summary>
        /// Refreshes the list of plugins from the plugin manager.
        /// </summary>
        private void RefreshPluginsList()
        {
            if (_pluginManager == null) return;

            Plugins.Clear();
            RecentlyUpdatedPlugins.Clear();

            foreach (var plugin in _pluginManager.LoadedPlugins)
            {
                try
                {
                    var meta = plugin.GetMetadata();
                    var viewModel = new PluginViewModel
                    {
                        PluginName = meta.Name,
                        PluginVersion = meta.Version,
                        PluginAuthor = meta.Author,
                        PluginDescription = meta.Description,
                        PluginIsEnabled = true,
                        PluginStatus = "Enabled"
                    };

                    Plugins.Add(viewModel);

                    // Add to recently updated (just for demonstration)
                    RecentlyUpdatedPlugins.Add(viewModel);
                }
                catch (Exception ex)
                {
                    // Log the error but continue processing other plugins
                    StatusMessage = $"Error loading plugin: {ex.Message}";
                }
            }
        }

        [RelayCommand]
        private async Task InstallPluginFromFile()
        {
            IsLoading = true;
            StatusMessage = "Installing plugin...";

            // In a real implementation, this would show a file dialog
            // and install the selected plugin.
            await Task.Delay(1000); // Simulate installation delay

            // Refresh the plugins list
            RefreshPluginsList();

            IsLoading = false;
            StatusMessage = "Plugin installed successfully.";
        }

        [RelayCommand]
        private void RefreshPlugins()
        {
            RefreshPluginsList();
            StatusMessage = "Plugins refreshed.";
        }

        [RelayCommand]
        private void UninstallSelectedPlugin()
        {
            if (_pluginManager == null || SelectedPlugin == null)
            {
                StatusMessage = "No plugin selected or plugin manager not available.";
                return;
            }

            // Uninstall the selected plugin
            if (SelectedPlugin != null)
            {
                _pluginManager.UnloadPlugin(SelectedPlugin.PluginName);
                
                // Refresh the list
                RefreshPluginsList();
                
                StatusMessage = $"Plugin '{SelectedPlugin.PluginName}' uninstalled.";
                SelectedPlugin = null;
            }
        }
    }
}
