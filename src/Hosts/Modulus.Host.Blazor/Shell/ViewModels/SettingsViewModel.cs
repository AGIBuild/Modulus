using CommunityToolkit.Mvvm.ComponentModel;
using Modulus.UI.Abstractions;
using UiAppTheme = Modulus.UI.Abstractions.AppTheme;

namespace Modulus.Host.Blazor.Shell.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    private readonly IThemeService? _themeService;

    [ObservableProperty]
    private UiAppTheme _selectedTheme = UiAppTheme.System;

    public SettingsViewModel(IThemeService? themeService = null)
    {
        _themeService = themeService;
        Title = "Settings";

        if (_themeService != null)
        {
            SelectedTheme = _themeService.CurrentTheme;
            _themeService.ThemeChanged += OnThemeChanged;
        }
    }

    private void OnThemeChanged(object? sender, UiAppTheme theme)
    {
        SelectedTheme = theme;
    }

    public void SetTheme(UiAppTheme theme)
    {
        _themeService?.SetTheme(theme);
    }
}
