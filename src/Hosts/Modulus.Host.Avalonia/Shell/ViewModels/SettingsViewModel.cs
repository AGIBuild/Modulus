using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Modulus.UI.Abstractions;
using System.Collections.Generic;

namespace Modulus.Host.Avalonia.Shell.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    private readonly IThemeService? _themeService;

    [ObservableProperty]
    private ThemeOption? _selectedTheme;

    public List<ThemeOption> ThemeOptions { get; } = new()
    {
        new ThemeOption("System", "💻 Follow System", AppTheme.System),
        new ThemeOption("Light", "☀️ Light", AppTheme.Light),
        new ThemeOption("Dark", "🌙 Dark", AppTheme.Dark)
    };

    public SettingsViewModel(IThemeService? themeService = null)
    {
        _themeService = themeService;
        Title = "Settings";
        
        if (_themeService != null)
        {
            UpdateThemeState(_themeService.CurrentTheme);
            _themeService.ThemeChanged += OnThemeChanged;
        }
        else
        {
            SelectedTheme = ThemeOptions[0];
        }
    }

    private void OnThemeChanged(object? sender, AppTheme theme)
    {
        UpdateThemeState(theme);
    }

    private void UpdateThemeState(AppTheme theme)
    {
        SelectedTheme = ThemeOptions.Find(t => t.Theme == theme) ?? ThemeOptions[0];
    }

    partial void OnSelectedThemeChanged(ThemeOption? value)
    {
        if (value != null && _themeService != null)
        {
            _themeService.SetTheme(value.Theme);
        }
    }
}

public class ThemeOption
{
    public string Id { get; }
    public string DisplayName { get; }
    public AppTheme Theme { get; }

    public ThemeOption(string id, string displayName, AppTheme theme)
    {
        Id = id;
        DisplayName = displayName;
        Theme = theme;
    }

    public override string ToString() => DisplayName;
}
