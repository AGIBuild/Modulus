using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Modulus.Plugin.Abstractions;
using Modulus.PluginHost;

namespace Modulus.App.Services
{
    public class PluginService : IPluginService
    {
        private readonly IPluginManager _pluginManager;
        private readonly string _pluginDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Modulus", "Plugins");

        public PluginService(IPluginManager pluginManager)
        {
            _pluginManager = pluginManager;
            if (!Directory.Exists(_pluginDirectory))
            {
                Directory.CreateDirectory(_pluginDirectory);
            }
        }

        public async Task InstallPluginAsync(string pluginPath)
        {
            // Basic implementation: copy plugin to plugin directory and load
            // In a real app, you'd want more robust error handling, version checks, etc.
            if (string.IsNullOrEmpty(pluginPath) || !File.Exists(pluginPath))
            {
                throw new ArgumentException("Invalid plugin path", nameof(pluginPath));
            }

            var pluginFileName = Path.GetFileName(pluginPath);
            var destinationPath = Path.Combine(_pluginDirectory, pluginFileName);

            File.Copy(pluginPath, destinationPath, true); // Overwrite if exists

            // Use the async version of LoadPlugin
            await _pluginManager.LoadPluginAsync(destinationPath);
        }

        public async Task UninstallPluginAsync(string pluginName)
        {
            // Basic implementation: find plugin, unload, and delete its files
            // This is a simplified example. Proper uninstallation is complex and needs to handle 
            // shared resources, running code, etc.
            var plugin = FindPluginByName(pluginName);
            if (plugin != null)
            {
                var meta = plugin.GetMetadata();
                // Use the async version of UnloadPlugin
                await _pluginManager.UnloadPluginAsync(meta.Name);

                // Attempt to delete plugin files - this is risky if files are locked
                // A more robust solution might involve a restart or delayed deletion.
                // Also, need to know the exact files associated with the plugin.
                // For now, let's assume the plugin is a single DLL in the _pluginDirectory.
                // This needs to be more robust based on how plugins are packaged and identified.
                var pluginFiles = Directory.GetFiles(_pluginDirectory, $"{meta.Name}.*.dll"); // Example pattern
                foreach (var file in pluginFiles)
                {
                    try
                    {
                        File.Delete(file);
                    }
                    catch (IOException ex)
                    {
                        // Log or handle file in use, etc.
                        Console.WriteLine($"Error deleting plugin file {file}: {ex.Message}");
                    }
                }
            }
            else
            {
                throw new InvalidOperationException($"Plugin '{pluginName}' not found.");
            }
            // Removed await Task.CompletedTask as the main operations are now awaited.
        }

        public async Task EnablePluginAsync(string pluginName)
        {
            // This depends on how your PluginManager handles enabling/disabling
            // It might involve reloading the plugin or setting a flag that prevents it from being activated.
            var plugin = FindPluginByName(pluginName);
            if (plugin != null)
            {
                if (!_pluginManager.IsPluginLoaded(plugin.GetMetadata().Name))
                {
                     // Use the async version of LoadPlugin
                     await _pluginManager.LoadPluginAsync(Path.Combine(_pluginDirectory, plugin.GetMetadata().Name + ".dll")); 
                }
            }
            else
            {
                throw new InvalidOperationException($"Plugin '{pluginName}' not found for enabling.");
            }
            // Removed await Task.CompletedTask
        }

        public async Task DisablePluginAsync(string pluginName)
        {
            // Similar to Enable, depends on PluginManager implementation
            var plugin = FindPluginByName(pluginName);
            if (plugin != null)
            {
                // Use the async version of UnloadPlugin
                await _pluginManager.UnloadPluginAsync(plugin.GetMetadata().Name);
            }
            else
            {
                throw new InvalidOperationException($"Plugin '{pluginName}' not found for disabling.");
            }
            // Removed await Task.CompletedTask
        }

        public bool IsPluginEnabled(string pluginName)
        {
            // Depends on how enabled/disabled state is tracked.
            // For now, let's assume a plugin is enabled if it's loaded.
            var plugin = FindPluginByName(pluginName);
            return plugin != null && _pluginManager.IsPluginLoaded(plugin.GetMetadata().Name); // Assuming IsPluginLoaded exists
        }

        private IPlugin? FindPluginByName(string pluginName)
        {
            return _pluginManager.LoadedPlugins.FirstOrDefault(p => p.GetMetadata().Name.Equals(pluginName, StringComparison.OrdinalIgnoreCase));
        }
    }
}
