using Microsoft.Extensions.Configuration;
using Modulus.App.Services;

namespace Modulus.App.ViewModels
{
    public class AboutViewModel : NavigationViewModelBase
    {
        private readonly IConfiguration _configuration;
        
        public override string ViewName => "AboutView";

        public string Version { get; }
        
        public AboutViewModel(IConfiguration configuration, INavigationService? navigationService = null) 
            : base(navigationService)
        {
            _configuration = configuration;
            Version = _configuration["App:Version"] ?? "1.0.0";
        }
    }
}
