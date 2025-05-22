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
        private MainViewModel? _mainViewModel;
        private readonly Dictionary<string, IPlugin> _pluginLookup = new();

        /// <summary>
        /// Sets the main view model for the application so the plugin views can be displayed.
        /// </summary>
        /// <param name="mainViewModel">The main view model for the application.</param>
        public void SetMainViewModel(MainViewModel mainViewModel)
        {
            _mainViewModel = mainViewModel;
        }

        /// <summary>
        /// Adds plugin navigation items to the appropriate section of the NavigationView.
        /// </summary>
        /// <param name="plugins">The collection of plugins to add navigation items for.</param>
        public void AddPluginNavigationItems(IEnumerable<IPlugin> plugins)
        {
            if (_mainViewModel == null || plugins == null) return;
            
            foreach (var plugin in plugins)
            {
                var meta = GetPluginMetadata(plugin);
                if (meta == null) continue;

                // Skip plugins that don't have navigation icon
                if (string.IsNullOrEmpty(meta.NavigationIcon)) continue;

                // Add or update the plugin lookup
                _pluginLookup[meta.Name] = plugin;

                // 添加导航菜单项 - 使用Name属性作为显示名称的备选
                string displayName = GetDisplayName(meta);
                _mainViewModel.Navigation.AddNavigationItem(
                    displayName, 
                    meta.NavigationIcon, 
                    $"Plugin_{meta.Name}", 
                    "body");
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
