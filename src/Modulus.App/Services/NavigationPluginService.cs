using Modulus.App.Controls.ViewModels;
using Modulus.App.ViewModels;
using Modulus.Plugin.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Modulus.App.Services
{
    /// <summary>
    /// Service for managing plugin navigation items in the NavigationBar.
    /// </summary>
    public class NavigationPluginService
    {
        private readonly NavigationBarViewModel _navigationBar;
        private MainViewModel? _mainViewModel;
        private readonly Dictionary<string, IPlugin> _pluginLookup = new();

        public NavigationPluginService(NavigationBarViewModel navigationBar)
        {
            _navigationBar = navigationBar ?? throw new ArgumentNullException(nameof(navigationBar));
        }

        /// <summary>
        /// Safely gets the plugin metadata, handling potential interface mismatches.
        /// </summary>
        private IPluginMeta? GetPluginMetadataSafe(IPlugin plugin)
        {
            try
            {
                // Try direct interface call first
                return plugin.GetMetadata();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting plugin metadata: {ex.Message}");
                
                try
                {
                    // Fallback to reflection if the interface method call fails
                    var method = plugin.GetType().GetMethod("GetMetadata", BindingFlags.Public | BindingFlags.Instance);
                    if (method != null)
                    {
                        var result = method.Invoke(plugin, null);
                        if (result is IPluginMeta meta)
                        {
                            return meta;
                        }
                    }
                }
                catch
                {
                    // Ignore reflection errors
                }
                
                return null;
            }
        }

        /// <summary>
        /// Sets the main view model for the application so the plugin views can be displayed.
        /// </summary>
        /// <param name="mainViewModel">The main view model for the application.</param>
        public void SetMainViewModel(MainViewModel mainViewModel)
        {
            _mainViewModel = mainViewModel;
        }

        /// <summary>
        /// Adds plugin navigation items to the appropriate section of the NavigationBar.
        /// </summary>
        /// <param name="plugins">The collection of plugins to add navigation items for.</param>
        public void AddPluginNavigationItems(IEnumerable<IPlugin> plugins)
        {
            if (plugins == null) return;
            
            foreach (var plugin in plugins)
            {
                var meta = GetPluginMetadataSafe(plugin);
                if (meta == null) continue;

                // Skip plugins that don't have navigation icon
                if (string.IsNullOrEmpty(meta.NavigationIcon)) continue;

                // Add or update the plugin lookup
                _pluginLookup[meta.Name] = plugin;

                var navItem = new NavigationMenuItemViewModel
                {
                    Icon = meta.NavigationIcon,
                    Tooltip = meta.Name,
                    Command = new CommunityToolkit.Mvvm.Input.RelayCommand(() => ActivatePlugin(plugin)),
                    Width = 56,
                    Height = 56,
                    CornerRadius = 12,
                    FontSize = 26
                };

                // Determine which section to add the plugin to based on metadata
                if (meta.NavigationSection?.ToLower() == "header")
                {
                    _navigationBar.HeaderItems.Add(navItem);
                }
                else if (meta.NavigationSection?.ToLower() == "footer")
                {
                    _navigationBar.FooterItems.Add(navItem);
                }
                else
                {
                    // Default to body section
                    _navigationBar.BodyItems.Add(navItem);
                }
            }
        }

        /// <summary>
        /// Removes plugin navigation items from all sections of the NavigationBar.
        /// </summary>
        /// <param name="pluginId">The ID of the plugin to remove navigation items for.</param>
        public void RemovePluginNavigationItems(string pluginId)
        {
            if (string.IsNullOrEmpty(pluginId)) return;

            RemoveMatchingItems(_navigationBar.HeaderItems, pluginId);
            RemoveMatchingItems(_navigationBar.BodyItems, pluginId);
            RemoveMatchingItems(_navigationBar.FooterItems, pluginId);
            
            // Remove from plugin lookup
            if (_pluginLookup.ContainsKey(pluginId))
            {
                _pluginLookup.Remove(pluginId);
            }
        }

        private void RemoveMatchingItems(ICollection<NavigationMenuItemViewModel> items, string pluginId)
        {
            var itemsToRemove = items
                .Where(item => item.Tooltip == pluginId)
                .ToList();

            foreach (var item in itemsToRemove)
            {
                items.Remove(item);
            }
        }

        private void ActivatePlugin(IPlugin plugin)
        {
            if (_mainViewModel == null) return;
            
            // Clear all active states
            foreach (var item in _navigationBar.HeaderItems) item.IsActive = false;
            foreach (var item in _navigationBar.BodyItems) item.IsActive = false;
            foreach (var item in _navigationBar.FooterItems) item.IsActive = false;
            
            // Deactivate all built-in views
            _mainViewModel.SetActive(string.Empty);

            // Set active state for this plugin's navigation item
            var meta = GetPluginMetadataSafe(plugin);
            if (meta == null) return;
            
            // Find the item in any section and activate it
            var items = _navigationBar.HeaderItems
                .Concat(_navigationBar.BodyItems)
                .Concat(_navigationBar.FooterItems);
                
            var navItem = items.FirstOrDefault(item => item.Tooltip == meta.Name);
            if (navItem != null)
            {
                navItem.IsActive = true;
            }
            
            // Get the plugin view and display it in the main content area
            var pluginView = plugin.GetMainView();
            if (pluginView != null)
            {
                _mainViewModel.CurrentView = new PluginContainerViewModel
                {
                    PluginName = meta.Name,
                    PluginView = pluginView
                };
            }
        }
    }
}
