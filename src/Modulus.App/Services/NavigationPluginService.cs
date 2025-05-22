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
    /// Service for managing plugin navigation items in the NavigationView.
    /// </summary>
    public class NavigationPluginService
    {
        private MainWindowViewModel? _mainViewModel;
        private readonly Dictionary<string, IPlugin> _pluginLookup = new();

        /// <summary>
        /// Sets the main view model for the application so the plugin views can be displayed.
        /// </summary>
        /// <param name="mainViewModel">The main view model for the application.</param>
        public void SetMainViewModel(MainWindowViewModel mainViewModel)
        {
            _mainViewModel = mainViewModel;
        }        /// <summary>
                 /// Adds plugin navigation items to the appropriate section of the NavigationView.
                 /// </summary>
                 /// <param name="plugins">The collection of plugins to add navigation items for.</param>
        public void AddPluginNavigationItems(IEnumerable<IPlugin> plugins)
        {
            if (_mainViewModel == null) throw new InvalidOperationException("MainViewModel must be set before adding plugin items");
            if (plugins == null) return;

            foreach (var plugin in plugins)
            {
                try
                {
                    // Validate plugin and metadata
                    if (plugin == null)
                    {
                        System.Diagnostics.Debug.WriteLine("Skipping null plugin");
                        continue;
                    }

                    var meta = GetPluginMetadata(plugin);
                    if (meta == null)
                    {
                        System.Diagnostics.Debug.WriteLine($"Plugin metadata is null");
                        continue;
                    }

                    if (string.IsNullOrEmpty(meta.Name))
                    {
                        System.Diagnostics.Debug.WriteLine("Plugin name is required");
                        continue;
                    }

                    // Add or update the plugin lookup
                    _pluginLookup[meta.Name] = plugin;

                    // Add navigation menu item
                    var viewName = $"Plugin_{meta.Name}";
                    _mainViewModel.Navigation.AddNavigationItem(
                        meta.Name,
                        meta.NavigationIcon ?? "\uE8A5", // Default to generic plugin icon
                        viewName,
                        meta.NavigationSection ?? "body"
                    );

                    // Register click handler if needed
                    var item = _mainViewModel.Navigation.GetNavigationItem(viewName);
                    if (item != null)
                    {
                        item.Parameter = new NavigationPageInfo
                        {
                            Title = meta.Name,
                            Icon = meta.NavigationIcon ?? "\uE8A5"
                        };
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"添加插件导航项时出错: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 获取插件的显示名称，支持不同版本的接口
        /// </summary>
        private string GetDisplayName(IPluginMeta meta)
        {
            // 尝试通过反射获取DisplayName属性
            var property = meta.GetType().GetProperty("DisplayName");
            if (property != null)
            {
                var value = property.GetValue(meta) as string;
                if (!string.IsNullOrEmpty(value))
                {
                    return value;
                }
            }

            // 回退到Name属性
            return meta.Name;
        }

        /// <summary>
        /// Removes plugin navigation items from the NavigationView.
        /// </summary>
        /// <param name="pluginId">The ID of the plugin to remove navigation items for.</param>
        public void RemovePluginNavigationItems(string pluginId)
        {
            if (string.IsNullOrEmpty(pluginId) || _mainViewModel == null) return;

            // 移除导航菜单项
            var items = _mainViewModel.Navigation.NavigationItems
                .Where(i => i.ViewName == $"Plugin_{pluginId}")
                .ToList();

            foreach (var item in items)
            {
                _mainViewModel.Navigation.NavigationItems.Remove(item);
            }

            // Remove from plugin lookup
            if (_pluginLookup.ContainsKey(pluginId))
            {
                _pluginLookup.Remove(pluginId);
            }
        }

        /// <summary>
        /// 获取插件元数据
        /// </summary>
        private IPluginMeta? GetPluginMetadata(IPlugin plugin)
        {
            try
            {
                return plugin.GetMetadata();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"获取插件元数据失败: {ex.Message}");
                return null;
            }
        }
    }
}
