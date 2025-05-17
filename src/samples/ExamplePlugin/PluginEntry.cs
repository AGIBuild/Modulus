using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Modulus.Plugin.Abstractions;
using ExamplePlugin.Services;
using ExamplePlugin.Extensions;

namespace ExamplePlugin
{
    /// <summary>
    /// Example plugin demonstrating the Modulus plugin contract.
    /// </summary>
    public class PluginEntry : IPlugin
    {
        private readonly ExamplePluginMeta _metadata = new();

        public IPluginMeta GetMetadata() => _metadata;

        public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            // Register plugin-specific services using our extension method
            services.AddExamplePluginServices();
        }

        public void Initialize(IServiceProvider provider)
        {
            // Resolve logger and log plugin initialization
            var logger = provider.GetService<ILogger<PluginEntry>>();
            logger?.LogInformation("ExamplePlugin initialized");
            
            // Get our service and call it
            var myService = provider.GetService<IMyService>();
            if (myService != null)
            {
                logger?.LogInformation(myService.GetMessage());
            }
        }

        public object? GetMainView() 
        {
            // Return our example view
            return new Views.ExampleView();
        }
        
        public object? GetMenu() => null; // No menu for this example
    }

    /// <summary>
    /// Metadata for the example plugin
    /// </summary>
    public class ExamplePluginMeta : IPluginMeta
    {
        public string Name => "ExamplePlugin";
        public string Version => "1.0.0";
        public string Description => "A sample plugin for Modulus.";
        public string Author => "Modulus Team";
        public string[]? Dependencies => null;
        public string ContractVersion => "2.0.0";
        public string? NavigationIcon => "\uE710"; // Document icon
        public string? NavigationSection => "body";
        public int NavigationOrder => 100;
    }
}
