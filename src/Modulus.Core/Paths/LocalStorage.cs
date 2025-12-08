using System.Runtime.InteropServices;

namespace Modulus.Core.Paths;

/// <summary>
/// Provides user-scoped storage locations for data and logs.
/// </summary>
public static class LocalStorage
{
    /// <summary>
    /// Returns the user-level Modulus root directory (created if missing).
    /// Windows: %AppData%/Modulus; others: $HOME/.modulus
    /// </summary>
    public static string GetUserRoot()
    {
        string root;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            root = Path.Combine(appData, "Modulus");
        }
        else
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            root = string.IsNullOrWhiteSpace(home)
                ? Path.Combine(".", ".modulus")
                : Path.Combine(home, ".modulus");
        }

        Directory.CreateDirectory(root);
        return root;
    }

    /// <summary>
    /// Returns the logs directory for a host (created if missing).
    /// </summary>
    public static string GetLogsRoot(string hostType)
    {
        var root = Path.Combine(GetUserRoot(), "Logs", SanitizeSegment(hostType));
        Directory.CreateDirectory(root);
        return root;
    }

    /// <summary>
    /// Returns the default database file path for a logical name under the user root.
    /// </summary>
    public static string GetDatabasePath(string databaseName)
    {
        var safeName = string.IsNullOrWhiteSpace(databaseName) ? "Modulus" : SanitizeSegment(databaseName);
        var root = GetUserRoot();
        var filePath = Path.Combine(root, $"{safeName}.db");
        var dir = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrWhiteSpace(dir))
        {
            Directory.CreateDirectory(dir);
        }
        return filePath;
    }

    private static string SanitizeSegment(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var cleaned = new string(value.Select(c => invalid.Contains(c) ? '-' : c).ToArray());
        return string.IsNullOrWhiteSpace(cleaned) ? "host" : cleaned;
    }
}

