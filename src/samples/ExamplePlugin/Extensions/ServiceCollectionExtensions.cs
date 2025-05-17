using Microsoft.Extensions.DependencyInjection;
using ExamplePlugin.Services;

namespace ExamplePlugin.Extensions
{
    /// <summary>
    /// Extension methods for service collection
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds all ExamplePlugin services to the service collection
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddExamplePluginServices(this IServiceCollection services)
        {
            // Register all plugin services
            services.AddSingleton<IMyService, MyService>();
            
            return services;
        }
    }
}
