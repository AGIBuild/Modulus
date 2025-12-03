using Avalonia;
using Avalonia.Styling;
using Modulus.Core.Data;
using Modulus.UI.Abstractions;
using System;
using System.Threading.Tasks;

namespace Modulus.Host.Avalonia.Services;

/// <summary>
/// Avalonia implementation of IThemeService with persistence.
/// </summary>
public class AvaloniaThemeService : IThemeService
{
    private readonly ISettingsService _settingsService;
    private AppTheme _currentTheme = AppTheme.System;
    private bool _initialized;
    
    public AvaloniaThemeService(ISettingsService settingsService)
    {
        _settingsService = settingsService;
    }
    
    public AppTheme CurrentTheme => _currentTheme;
    
    public event EventHandler<AppTheme>? ThemeChanged;

    /// <summary>
    /// Loads the saved theme from database. Call this during app initialization.
    /// </summary>
    public async Task InitializeAsync()
    {
        if (_initialized) return;
        
        var savedTheme = await _settingsService.GetAsync<string>(SettingKeys.Theme);
        if (!string.IsNullOrEmpty(savedTheme) && Enum.TryParse<AppTheme>(savedTheme, out var theme))
        {
            _currentTheme = theme;
            ApplyTheme(theme);
        }
        _initialized = true;
    }

    public void SetTheme(AppTheme theme)
    {
        if (_currentTheme == theme) return;
        
        _currentTheme = theme;
        ApplyTheme(theme);
        ThemeChanged?.Invoke(this, theme);
        
        // Persist to database (fire and forget)
        _ = _settingsService.SetAsync(SettingKeys.Theme, theme.ToString());
    }

    private void ApplyTheme(AppTheme theme)
    {
        var app = Application.Current;
        if (app == null) return;

        app.RequestedThemeVariant = theme switch
        {
            AppTheme.Light => ThemeVariant.Light,
            AppTheme.Dark => ThemeVariant.Dark,
            _ => ThemeVariant.Default // System
        };
    }

    public void ToggleTheme()
    {
        var app = Application.Current;
        if (app == null) return;

        // Determine actual current theme
        var actualTheme = app.ActualThemeVariant;
        
        if (actualTheme == ThemeVariant.Dark)
        {
            SetTheme(AppTheme.Light);
        }
        else
        {
            SetTheme(AppTheme.Dark);
        }
    }
}

