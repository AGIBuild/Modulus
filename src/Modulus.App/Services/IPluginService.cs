using System.Threading.Tasks;

namespace Modulus.App.Services
{
    public interface IPluginService
    {
        Task InstallPluginAsync(string pluginPath);
        Task UninstallPluginAsync(string pluginName);
        Task EnablePluginAsync(string pluginName);
        Task DisablePluginAsync(string pluginName);
        bool IsPluginEnabled(string pluginName);
        // Potentially add methods to get plugin status, list plugins, etc.
    }
}
