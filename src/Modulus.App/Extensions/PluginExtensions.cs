using Modulus.Plugin.Abstractions;
using System;
using System.Reflection;

namespace Modulus.App.Extensions
{
    /// <summary>
    /// Extension methods for working with plugins.
    /// </summary>
    public static class PluginExtensions
    {
        /// <summary>
        /// Safely gets the plugin metadata, handling potential interface mismatches
        /// through reflection if necessary.
        /// </summary>
        /// <param name="plugin">The plugin to get metadata from.</param>
        /// <returns>The plugin metadata or null if metadata cannot be retrieved.</returns>
        public static IPluginMeta? GetPluginMetadataSafe(this IPlugin plugin)
        {
            try
            {
                // Try direct interface call first
                return plugin.GetMetadata();
            }
            catch (Exception)
            {
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
                    
                    // If we can't get metadata, create a default one based on the type name
                    return new DefaultPluginMeta(plugin.GetType().Name);
                }
                catch
                {
                    // Last resort fallback
                    return new DefaultPluginMeta(plugin.GetType().Name);
                }
            }
        }
    }

    /// <summary>
    /// Default implementation of IPluginMeta for when metadata cannot be retrieved.
    /// </summary>
    public class DefaultPluginMeta : IPluginMeta
    {
        public DefaultPluginMeta(string typeName)
        {
            Name = typeName ?? "Unknown Plugin";
        }

        public string Name { get; }
        public string Version => "1.0.0";
        public string Description => "Plugin with unknown metadata";
        public string Author => "Unknown";
        public string[]? Dependencies => null;
        public string ContractVersion => "1.0.0";
        public string? NavigationIcon => "\uE783"; // Default icon
        public string? NavigationSection => "body";
        public int NavigationOrder => 999;
    }
}
