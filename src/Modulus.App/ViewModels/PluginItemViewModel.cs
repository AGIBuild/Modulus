using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Modulus.App.Services;
using Modulus.Plugin.Abstractions;
using System.Threading.Tasks;

namespace Modulus.App.ViewModels
{
    public partial class PluginItemViewModel : ObservableObject
    {
        private readonly IPluginMeta _pluginMeta;
        private readonly IPluginService _pluginService;

        [ObservableProperty]
        private bool _isEnabled;

        public string Name => _pluginMeta.Name;
        public string Version => _pluginMeta.Version;
        public string Author => _pluginMeta.Author;
        public string Description => _pluginMeta.Description;
        public string[]? Dependencies => _pluginMeta.Dependencies;
        public string ContractVersion => _pluginMeta.ContractVersion;
        public string? NavigationIcon => _pluginMeta.NavigationIcon;
        public string? NavigationSection => _pluginMeta.NavigationSection;
        public int NavigationOrder => _pluginMeta.NavigationOrder;

        public PluginItemViewModel(IPluginMeta pluginMeta, bool isEnabled, IPluginService pluginService)
        {
            _pluginMeta = pluginMeta;
            _isEnabled = isEnabled;
            _pluginService = pluginService;
        }

        [RelayCommand]
        private async Task UninstallAsync()
        {
            await _pluginService.UninstallPluginAsync(Name);
        }

        partial void OnIsEnabledChanged(bool value)
        {
            _ = Task.Run(async () =>
            {
                if (value)
                {
                    await _pluginService.EnablePluginAsync(Name);
                }
                else
                {
                    await _pluginService.DisablePluginAsync(Name);
                }
            });
        }
    }
}
