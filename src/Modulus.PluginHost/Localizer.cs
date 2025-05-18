using Microsoft.Extensions.Logging;

namespace Modulus.PluginHost
{
    /// <summary>
    /// Interface for plugin localization
    /// </summary>
    public interface ILocalizer
    {
        /// <summary>
        /// Get localized string for the given key
        /// </summary>
        string GetString(string key);
        
        /// <summary>
        /// Get localized string for the given key with format arguments
        /// </summary>
        string GetString(string key, params object[] args);
    }

    /// <summary>
    /// Basic implementation of ILocalizer for plugins
    /// </summary>
    public class PluginLocalizer : ILocalizer
    {
        private readonly string _pluginDir;
        private readonly ILogger<PluginLocalizer>? _logger;

        public PluginLocalizer(string pluginDir, ILogger<PluginLocalizer>? logger = null)
        {
            _pluginDir = pluginDir;
            _logger = logger;
        }

        public string GetString(string key)
        {
            // Simple implementation for now - could load from resource files later
            _logger?.LogDebug("Getting localized string for key: {Key}", key);
            return key;
        }

        public string GetString(string key, params object[] args)
        {
            var value = GetString(key);
            return string.Format(value, args);
        }
    }
}