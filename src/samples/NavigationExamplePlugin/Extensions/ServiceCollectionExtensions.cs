using Microsoft.Extensions.DependencyInjection;
using NavigationExamplePlugin.Services;

namespace NavigationExamplePlugin.Extensions
{
    /// <summary>
    /// Extension methods for registering plugin services with the DI container
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds all NavigationExamplePlugin services to the service collection
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddNavigationExampleServices(this IServiceCollection services)
        {
            // Register all plugin services
            services.AddSingleton<INavigationService, NavigationService>();
            
            return services;
        }
    }
}
