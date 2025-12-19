using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Modulus.Sdk;
using Modulus.UI.Abstractions;

namespace Modulus.Modules.ModulusModule.ViewModels;

[AvaloniaViewMenu("main", "Main", Icon = IconKind.Folder, Order = 100)]
public partial class MainViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _message = "Hello from ModulusModule!";

    public MainViewModel()
    {
        Title = "{{DisplayNameComputed}}";
    }

    [RelayCommand]
    private void SayHello()
    {
        Message = $"Hello at {DateTime.Now:HH:mm:ss}";
    }

    public override Task<bool> CanNavigateFromAsync(NavigationContext context)
    {
        return Task.FromResult(true);
    }

    public override Task OnNavigatedToAsync(NavigationContext context)
    {
        return Task.CompletedTask;
    }
}

