using System;

namespace NavigationExamplePlugin.Services
{
    /// <summary>
    /// Interface for the navigation service
    /// </summary>
    public interface INavigationService
    {
        /// <summary>
        /// Navigates to a specific view
        /// </summary>
        /// <param name="viewName">The name of the view to navigate to</param>
        void NavigateTo(string viewName);
    }

    /// <summary>
    /// Implementation of the navigation service for the plugin
    /// </summary>
    public class NavigationService : INavigationService
    {
        public NavigationService()
        {
            Console.WriteLine("NavigationService created");
        }

        public void NavigateTo(string viewName)
        {
            Console.WriteLine($"Navigating to {viewName}");
        }
    }
}
