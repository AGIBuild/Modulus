namespace ExamplePlugin.Services
{
    /// <summary>
    /// Simple service interface for the example plugin
    /// </summary>
    public interface IMyService
    {
        /// <summary>
        /// Gets a welcome message from the service
        /// </summary>
        /// <returns>A welcome message</returns>
        string GetMessage();
    }

    /// <summary>
    /// Implementation of the simple service
    /// </summary>
    public class MyService : IMyService
    {
        /// <summary>
        /// Gets a welcome message from the service
        /// </summary>
        /// <returns>A welcome message</returns>
        public string GetMessage() => "Hello from ExamplePlugin!";
    }
}
