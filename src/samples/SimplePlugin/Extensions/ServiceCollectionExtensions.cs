using Microsoft.Extensions.DependencyInjection;

namespace SimplePlugin.Extensions
{
    /// <summary>
    /// Extension methods for service collection
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds all SimplePlugin services to the service collection
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddSimplePluginServices(this IServiceCollection services)
        {
            // No services to register for this simple plugin
            return services;
        }
    }
}
