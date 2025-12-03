namespace Modulus.Core.Data;

/// <summary>
/// Service for managing application settings with type-safe access.
/// </summary>
public interface ISettingsService
{
    /// <summary>
    /// Gets a setting value.
    /// </summary>
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a setting value with a default fallback.
    /// </summary>
    Task<T> GetAsync<T>(string key, T defaultValue, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets a setting value.
    /// </summary>
    Task SetAsync<T>(string key, T value, CancellationToken cancellationToken = default);
}

/// <summary>
/// Well-known setting keys.
/// </summary>
public static class SettingKeys
{
    public const string Theme = "app.theme";
    public const string Language = "app.language";
    public const string SidebarCollapsed = "ui.sidebar.collapsed";
    public const string LastSelectedModule = "ui.lastSelectedModule";
}

