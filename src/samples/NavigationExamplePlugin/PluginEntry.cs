using Modulus.Plugin.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace NavigationExamplePlugin
{
    /// <summary>
    /// Example plugin that demonstrates navigation integration with the Modulus UI.
    /// </summary>
    public class PluginEntry : IPlugin
    {
        private readonly PluginMetadata _metadata;

        public PluginEntry()
        {
            _metadata = new PluginMetadata
            {
                Name = "Navigation Example",
                Version = "1.0.0",
                Description = "Demonstrates navigation integration with Modulus UI",
                Author = "Modulus Team",
                ContractVersion = "1.0.0",
                NavigationIcon = "\uE8A5", // Example icon from Segoe MDL2 Assets (chart icon)
                NavigationSection = "body", // Can be "header", "body", or "footer"
                NavigationOrder = 50
            };
        }

        public IPluginMeta GetMetadata() => _metadata;

        public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            // Register any dependencies needed by this plugin
            services.AddSingleton<Services.INavigationService, Services.NavigationService>();
        }

        public void Initialize(IServiceProvider provider)
        {
            // Plugin initialization logic
            Console.WriteLine("Navigation Example Plugin initialized");
        }

        public object? GetMainView()
        {
            // In a real implementation, this would return an Avalonia control
            // For this example, we'll just return a placeholder object
            return new { ViewType = "NavigationExampleView" };
        }

        public object? GetMenu()
        {
            // Optional menu extension
            return null;
        }
    }

    /// <summary>
    /// Implementation of IPluginMeta for the NavigationExamplePlugin.
    /// </summary>
    internal class PluginMetadata : IPluginMeta
    {
        public string Name { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string[]? Dependencies { get; set; }
        public string ContractVersion { get; set; } = string.Empty;
        public string? NavigationIcon { get; set; }
        public string? NavigationSection { get; set; } = "body";
        public int NavigationOrder { get; set; } = 100;
    }
}
