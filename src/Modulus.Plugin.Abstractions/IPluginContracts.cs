using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Modulus.Plugin.Abstractions
{
    /// <summary>
    /// Main plugin contract. All plugins must implement this interface.
    /// Provides metadata, DI, initialization, and UI extension points.
    /// </summary>
    public interface IPlugin
    {
        /// <summary>
        /// Gets the plugin metadata (name, version, author, etc).
        /// </summary>
        /// <returns>The plugin metadata.</returns>
        IPluginMeta GetMetadata();

        /// <summary>
        /// Register plugin services into the DI container.
        /// </summary>
        /// <param name="services">The service collection to register services into.</param>
        /// <param name="configuration">The configuration for the plugin.</param>
        void ConfigureServices(IServiceCollection services, IConfiguration configuration);
        
        /// <summary>
        /// Called after DI container is built. Use to resolve services and perform initialization.
        /// </summary>
        /// <param name="provider">The service provider for resolving dependencies.</param>
        void Initialize(IServiceProvider provider);
        
        /// <summary>
        /// Returns the main view/control for UI plugins (optional).
        /// </summary>
        object? GetMainView();
          /// <summary>
        /// Returns a menu or menu extension for the host (optional).
        /// </summary>
        object? GetMenu();
    }
    /// <summary>
    /// Plugin metadata contract. Describes plugin identity and contract version.
    /// </summary>
    public interface IPluginMeta
    {
        /// <summary>
        /// Plugin name.
        /// </summary>
        string Name { get; }
        
        /// <summary>
        /// Plugin version.
        /// </summary>
        string Version { get; }
        
        /// <summary>
        /// Plugin description.
        /// </summary>
        string Description { get; }
        
        /// <summary>
        /// Plugin author.
        /// </summary>
        string Author { get; }
        
        /// <summary>
        /// List of plugin dependencies (optional).
        /// </summary>
        string[]? Dependencies { get; }
        
        /// <summary>
        /// The contract version this plugin was built for.
        /// </summary>
        string ContractVersion { get; }
        
        /// <summary>
        /// Icon character for navigation menu (optional).
        /// This should be a character from an icon font like Segoe MDL2 Assets.
        /// </summary>
        string? NavigationIcon { get; }
        
        /// <summary>
        /// Section where the plugin should appear in the navigation bar (optional).
        /// Can be "header", "body", or "footer". Defaults to "body" if not specified.
        /// </summary>
        string? NavigationSection { get; }
        
        /// <summary>
        /// Order/position of the plugin in the navigation section (optional).
        /// Lower numbers appear first. Default is 100.
        /// </summary>
        int NavigationOrder { get; }
    }

    /// <summary>
    /// Localization contract for plugins. Provides access to localized resources and language switching.
    /// </summary>
    public interface ILocalizer
    {
        /// <summary>
        /// Gets a localized string by key.
        /// </summary>
        /// <param name="key">The resource key.</param>
        /// <returns>The localized string.</returns>
        string this[string key] { get; }
        /// <summary>
        /// The current language code (e.g. "en", "zh").
        /// </summary>
        string CurrentLanguage { get; }
        /// <summary>
        /// Switch the current language.
        /// </summary>
        /// <param name="lang">The language code to switch to.</param>
        void SetLanguage(string lang);
        /// <summary>
        /// List of supported language codes.
        /// </summary>
        IEnumerable<string> SupportedLanguages { get; }
    }

    /// <summary>
    /// Plugin configuration contract. Provides access to plugin-specific configuration.
    /// </summary>
    public interface IPluginSettings
    {
        /// <summary>
        /// The configuration for the plugin.
        /// </summary>
        IConfiguration Configuration { get; }
    }
}
