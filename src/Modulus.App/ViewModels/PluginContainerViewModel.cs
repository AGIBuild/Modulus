using CommunityToolkit.Mvvm.ComponentModel;

namespace Modulus.App.ViewModels
{
    /// <summary>
    /// ViewModel that wraps a plugin's main view for display in the application.
    /// </summary>
    public partial class PluginContainerViewModel : ObservableObject
    {
        /// <summary>
        /// The name of the plugin.
        /// </summary>
        [ObservableProperty]
        private string pluginName = string.Empty;

        /// <summary>
        /// The plugin's main view object.
        /// </summary>
        [ObservableProperty]
        private object? pluginView;
    }
}
