using Modulus.Core.Data;
using Modulus.UI.Abstractions;
using UiAppTheme = Modulus.UI.Abstractions.AppTheme;

namespace Modulus.Host.Blazor.Shell.Services;

public class BlazorThemeService : IThemeService
{
    private readonly ISettingsService _settingsService;
    private UiAppTheme _currentTheme = UiAppTheme.System;
    private bool _initialized;

    public BlazorThemeService(ISettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    public UiAppTheme CurrentTheme => _currentTheme;

    public event EventHandler<UiAppTheme>? ThemeChanged;

    /// <summary>
    /// Loads the saved theme from database. Call this during app initialization.
    /// </summary>
    public async Task InitializeAsync()
    {
        if (_initialized) return;
        
        var savedTheme = await _settingsService.GetAsync<string>(SettingKeys.Theme);
        if (!string.IsNullOrEmpty(savedTheme) && Enum.TryParse<UiAppTheme>(savedTheme, out var theme))
        {
            _currentTheme = theme;
        }
        _initialized = true;
    }

    public void SetTheme(UiAppTheme theme)
    {
        if (_currentTheme != theme)
        {
            _currentTheme = theme;
            ThemeChanged?.Invoke(this, theme);
            
            // Persist to database (fire and forget)
            _ = _settingsService.SetAsync(SettingKeys.Theme, theme.ToString());
        }
    }

    public void ToggleTheme()
    {
        SetTheme(_currentTheme == UiAppTheme.Dark ? UiAppTheme.Light : UiAppTheme.Dark);
    }
}
