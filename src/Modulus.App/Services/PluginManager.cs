using Modulus.App.Controls.ViewModels;
using Modulus.Plugin.Abstractions;
using Modulus.PluginHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Reflection;

namespace Modulus.App.Services
{    /// <summary>
     /// Manages the loading, initialization, and integration of plugins with the Modulus UI.
     /// </summary>
    public class PluginManager
    {
        private readonly NavigationBarViewModel _navigationBarViewModel;
        private readonly NavigationPluginService _navigationPluginService;
        private readonly List<IPlugin> _loadedPlugins = new();
        private readonly PluginLoader _pluginLoader = new();
        private readonly IServiceCollection _services = new ServiceCollection();
        private IServiceProvider? _serviceProvider;

        public PluginManager(NavigationBarViewModel navigationBarViewModel, NavigationPluginService navigationPluginService)
        {
            _navigationBarViewModel = navigationBarViewModel ?? throw new ArgumentNullException(nameof(navigationBarViewModel));
            _navigationPluginService = navigationPluginService ?? throw new ArgumentNullException(nameof(navigationPluginService));

            // Register application services for plugins to use
            RegisterServices();
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
        /// Register services that plugins can use via DI
        /// </summary>
        private void RegisterServices()
        {
            // Add basic services for plugins
            _services.AddSingleton(_navigationBarViewModel);
            _services.AddSingleton(_navigationPluginService);

            // Build the service provider
            _serviceProvider = _services.BuildServiceProvider();
        }

        /// <summary>
        /// Gets a read-only list of currently loaded plugins.
        /// </summary>
        public IReadOnlyList<IPlugin> LoadedPlugins => _loadedPlugins.AsReadOnly();

        /// <summary>
        /// Loads plugins from the specified directory and integrates them with the UI.
        /// </summary>
        /// <param name="pluginDirectoryPath">Path to the directory containing plugins.</param>
        /// <returns>The number of plugins loaded.</returns>
        public async Task<int> LoadPluginsAsync(string pluginDirectoryPath)
        {            // Ensure we don't have any plugins loaded already
            UnloadAllPlugins();

            await Task.Run(() =>
            {
                // Use the proper plugin loader to load plugins from directory
                var pluginPaths = _pluginLoader.DiscoverPlugins();

                foreach (var pluginPath in pluginPaths)
                {
                    try
                    {
                        // Load and instantiate the plugin
                        var pluginInstance = _pluginLoader.RunPlugin(pluginPath);

                        if (pluginInstance is IPlugin plugin)
                        {
                            // Configure plugin services
                            var configuration = CreatePluginConfiguration(Path.GetDirectoryName(pluginPath) ?? pluginDirectoryPath);
                            plugin.ConfigureServices(_services, configuration);

                            // Initialize the plugin with services
                            plugin.Initialize(_serviceProvider!);

                            // Add to loaded plugins
                            _loadedPlugins.Add(plugin);
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log error but continue with other plugins
                        System.Diagnostics.Debug.WriteLine($"Error loading plugin: {ex.Message}");
                    }
                }
            });

            // After all plugins are loaded, refresh the UI
            RefreshPlugins();

            return _loadedPlugins.Count;
        }

        /// <summary>
        /// Creates a configuration object for a plugin based on its pluginsettings.json file.
        /// </summary>
        private IConfiguration CreatePluginConfiguration(string pluginDirectoryPath)
        {
            var configPath = Path.Combine(pluginDirectoryPath, "pluginsettings.json");

            var builder = new ConfigurationBuilder();

            if (File.Exists(configPath))
            {
                builder.AddJsonFile(configPath, optional: true, reloadOnChange: true);
            }

            return builder.Build();
        }

        /// <summary>
        /// Refreshes the plugins in the UI without reloading them.
        /// </summary>
        private void RefreshPlugins()
        {
            // Add all plugins to the navigation
            _navigationPluginService.AddPluginNavigationItems(_loadedPlugins);
        }        /// <summary>
                 /// Unloads all plugins and removes them from the UI.
                 /// </summary>
        public void UnloadAllPlugins()
        {
            foreach (var plugin in _loadedPlugins)
            {
                try
                {
                    var meta = GetPluginMetadataSafe(plugin);
                    if (meta != null)
                    {
                        _navigationPluginService.RemovePluginNavigationItems(meta.Name);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error unloading plugin: {ex.Message}");
                }
            }

            _loadedPlugins.Clear();
        }        /// <summary>
                 /// Unloads a specific plugin and removes it from the UI.
                 /// </summary>
                 /// <param name="pluginId">The ID/name of the plugin to unload.</param>
                 /// <returns>True if the plugin was found and unloaded, false otherwise.</returns>
        public bool UnloadPlugin(string pluginId)
        {
            try
            {
                var plugin = _loadedPlugins.FirstOrDefault(p =>
                {
                    var meta = GetPluginMetadataSafe(p);
                    return meta != null && meta.Name == pluginId;
                });

                if (plugin == null)
                    return false;

                _navigationPluginService.RemovePluginNavigationItems(pluginId);
                _loadedPlugins.Remove(plugin);
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error unloading plugin: {ex.Message}");
                return false;
            }
        }/// <summary>
         /// Adds test plugins for development purposes.
         /// </summary>
        public void AddTestPlugins()
        {
            // Load plugins from the samples directory
            var samplesDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "samples");
            Task.Run(() => LoadPluginsAsync(samplesDir)).Wait();
        }
    }
}
