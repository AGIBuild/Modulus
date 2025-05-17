using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Modulus.Plugin.Abstractions;
using SimplePlugin.Extensions;

namespace SimplePlugin
{
    /// <summary>
    /// A simple plugin for testing plugin loading.
    /// </summary>
    public class PluginEntry : IPlugin
    {
        private readonly SimplePluginMeta _metadata = new();

        public IPluginMeta GetMetadata() => _metadata;

        public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            // Add plugin services using extension method
            services.AddSimplePluginServices();
        }

        public void Initialize(IServiceProvider provider)
        {
            // Resolve logger and log plugin initialization
            var logger = provider.GetService<ILogger<PluginEntry>>();
            logger?.LogInformation("SimplePlugin initialized");
        }

        public object? GetMainView()
        {
            // Return our simple view
            return new Views.SimpleView();
        }

        public object? GetMenu() => null; // No menu for this plugin
    }

    /// <summary>
    /// Metadata for the simple plugin.
    /// </summary>
    public class SimplePluginMeta : IPluginMeta
    {
        public string Name => "Simple Plugin";
        public string Version => "1.0.0";
        public string Description => "A simple test plugin";
        public string Author => "Modulus Team";
        public string[]? Dependencies => null;
        public string ContractVersion => "2.0.0";
        public string? NavigationIcon => "\uE8A5";
        public string? NavigationSection => "body";
        public int NavigationOrder => 100;
    }
}
