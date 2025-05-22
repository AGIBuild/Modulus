using Modulus.App.Services;

namespace Modulus.App.ViewModels
{
    public class SettingsViewModel : NavigationViewModelBase
    {
        public override string ViewName => "SettingsView";

        public SettingsViewModel(INavigationService? navigationService = null) 
            : base(navigationService)
        {
        }
    }
}
