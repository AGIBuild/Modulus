using System.Text.Json;

namespace Modulus.Core.Data;

/// <summary>
/// Default implementation of ISettingsService using IAppDatabase.
/// </summary>
public class SettingsService : ISettingsService
{
    private readonly IAppDatabase _database;

    public SettingsService(IAppDatabase database)
    {
        _database = database;
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        var value = await _database.GetSettingAsync(key, cancellationToken).ConfigureAwait(false);
        if (value == null) return default;

        // Handle primitive types
        var targetType = typeof(T);
        if (targetType == typeof(string))
        {
            return (T)(object)value;
        }
        if (targetType == typeof(int))
        {
            return int.TryParse(value, out var intVal) ? (T)(object)intVal : default;
        }
        if (targetType == typeof(bool))
        {
            return bool.TryParse(value, out var boolVal) ? (T)(object)boolVal : default;
        }
        if (targetType == typeof(double))
        {
            return double.TryParse(value, out var doubleVal) ? (T)(object)doubleVal : default;
        }
        if (targetType.IsEnum)
        {
            return Enum.TryParse(targetType, value, true, out var enumVal) ? (T)enumVal! : default;
        }

        // Complex types: deserialize from JSON
        try
        {
            return JsonSerializer.Deserialize<T>(value);
        }
        catch
        {
            return default;
        }
    }

    public async Task<T> GetAsync<T>(string key, T defaultValue, CancellationToken cancellationToken = default)
    {
        var result = await GetAsync<T>(key, cancellationToken).ConfigureAwait(false);
        return result ?? defaultValue;
    }

    public async Task SetAsync<T>(string key, T value, CancellationToken cancellationToken = default)
    {
        string stringValue;
        var targetType = typeof(T);

        // Handle primitive types
        if (targetType == typeof(string))
        {
            stringValue = (string)(object)value!;
        }
        else if (targetType == typeof(int) || targetType == typeof(bool) || targetType == typeof(double))
        {
            stringValue = value?.ToString() ?? string.Empty;
        }
        else if (targetType.IsEnum)
        {
            stringValue = value?.ToString() ?? string.Empty;
        }
        else
        {
            // Complex types: serialize to JSON
            stringValue = JsonSerializer.Serialize(value);
        }

        await _database.SetSettingAsync(key, stringValue, cancellationToken).ConfigureAwait(false);
    }
}

