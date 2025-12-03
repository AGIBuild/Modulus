using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Modulus.UI.Abstractions;
using System.Threading.Tasks;

namespace Modulus.Modules.EchoPlugin.ViewModels;

public partial class EchoViewModel : ViewModelBase
{
    private readonly INotificationService? _notificationService;

    [ObservableProperty]
    private string _inputText = string.Empty;

    [ObservableProperty]
    private string _outputText = string.Empty;

    public EchoViewModel(INotificationService? notificationService = null)
    {
        _notificationService = notificationService;
        Title = "Echo Plugin";
    }

    [RelayCommand]
    private async Task EchoAsync()
    {
        if (string.IsNullOrWhiteSpace(InputText))
        {
            if (_notificationService != null)
            {
                await _notificationService.ShowErrorAsync("Input Required", "Please enter some text to echo.");
            }
            return;
        }

        OutputText = $"Echo: {InputText}";
        
        if (_notificationService != null)
        {
            await _notificationService.ShowInfoAsync("Echo", $"Echoed: {InputText}");
        }
    }
}

