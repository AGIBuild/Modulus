namespace Modulus.UI.Abstractions;

/// <summary>
/// Service for managing application theme.
/// </summary>
public interface IThemeService
{
    /// <summary>
    /// Gets the current theme.
    /// </summary>
    AppTheme CurrentTheme { get; }
    
    /// <summary>
    /// Sets the application theme.
    /// </summary>
    void SetTheme(AppTheme theme);
    
    /// <summary>
    /// Toggles between Light and Dark themes.
    /// </summary>
    void ToggleTheme();
    
    /// <summary>
    /// Event raised when the theme changes.
    /// </summary>
    event EventHandler<AppTheme>? ThemeChanged;
}

/// <summary>
/// Application theme options.
/// </summary>
public enum AppTheme
{
    /// <summary>
    /// Follow system theme.
    /// </summary>
    System,
    
    /// <summary>
    /// Light theme.
    /// </summary>
    Light,
    
    /// <summary>
    /// Dark theme.
    /// </summary>
    Dark
}

