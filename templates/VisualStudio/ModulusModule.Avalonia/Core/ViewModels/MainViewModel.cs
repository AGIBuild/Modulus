using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Modulus.UI.Abstractions;

namespace Modulus.Modules.$ext_safeprojectname$.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _message = "Hello from $ext_safeprojectname$!";

    public MainViewModel()
    {
        Title = "$ext_safeprojectname$";
    }

    [RelayCommand]
    private void SayHello()
    {
        Message = $"Hello at {DateTime.Now:HH:mm:ss}";
    }
}

