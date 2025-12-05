using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Avalonia.Styling;

namespace Modulus.UI.Avalonia.Infrastructure;

/// <summary>
/// Provides Avalonia resources from the Modulus.UI.Avalonia library using convention-based scanning.
/// Scans embedded resources under Themes/ to auto-discover styles and theme dictionaries.
/// </summary>
internal sealed class ModulusLibraryResourceProvider : IAvaloniaResourceProvider
{
    private const string AssemblyName = "Modulus.UI.Avalonia";
    private const string ThemesPrefix = "Themes/";
    private const string ControlsSubfolder = "Controls/";
    private const string LightSubfolder = "Light/";
    private const string DarkSubfolder = "Dark/";

    private static readonly Lazy<List<Uri>> CachedStyleUris = new(ScanStyleUris);
    private static readonly Lazy<List<AvaloniaThemeResourceDescriptor>> CachedThemeResources = new(ScanThemeResources);

    public IEnumerable<Uri> GetStyleUris() => CachedStyleUris.Value;

    public IEnumerable<AvaloniaThemeResourceDescriptor> GetThemeResources() => CachedThemeResources.Value;

    /// <summary>
    /// Scans assembly manifest for style resources under Themes/ (excluding Light/ and Dark/ subfolders).
    /// Convention: Themes/*.axaml and Themes/Controls/*.axaml are style resources.
    /// </summary>
    private static List<Uri> ScanStyleUris()
    {
        var result = new List<Uri>();
        var assembly = typeof(ModulusLibraryResourceProvider).Assembly;
        var resourceNames = GetAvaloniaResourcePaths(assembly);

        foreach (var path in resourceNames)
        {
            // Only include files under Themes/ but not in Light/ or Dark/ subfolders
            if (!path.StartsWith(ThemesPrefix, StringComparison.OrdinalIgnoreCase))
                continue;

            var relativePath = path[ThemesPrefix.Length..];

            // Skip theme-specific folders (they go to ThemeDictionaries)
            if (relativePath.StartsWith(LightSubfolder, StringComparison.OrdinalIgnoreCase) ||
                relativePath.StartsWith(DarkSubfolder, StringComparison.OrdinalIgnoreCase))
                continue;

            // Only .axaml files
            if (!path.EndsWith(".axaml", StringComparison.OrdinalIgnoreCase))
                continue;

            var uri = new Uri($"avares://{AssemblyName}/{path}");
            result.Add(uri);
        }

        // Ensure Generic.axaml is first (it includes other styles)
        result.Sort((a, b) =>
        {
            var aIsGeneric = a.AbsolutePath.EndsWith("Generic.axaml", StringComparison.OrdinalIgnoreCase);
            var bIsGeneric = b.AbsolutePath.EndsWith("Generic.axaml", StringComparison.OrdinalIgnoreCase);
            if (aIsGeneric && !bIsGeneric) return -1;
            if (!aIsGeneric && bIsGeneric) return 1;
            return string.Compare(a.AbsolutePath, b.AbsolutePath, StringComparison.OrdinalIgnoreCase);
        });

        return result;
    }

    /// <summary>
    /// Scans assembly manifest for theme-specific resources under Themes/Light/ and Themes/Dark/.
    /// Convention: Themes/Light/*.axaml → Light theme, Themes/Dark/*.axaml → Dark theme.
    /// </summary>
    private static List<AvaloniaThemeResourceDescriptor> ScanThemeResources()
    {
        var result = new List<AvaloniaThemeResourceDescriptor>();
        var assembly = typeof(ModulusLibraryResourceProvider).Assembly;
        var resourceNames = GetAvaloniaResourcePaths(assembly);

        foreach (var path in resourceNames)
        {
            if (!path.StartsWith(ThemesPrefix, StringComparison.OrdinalIgnoreCase))
                continue;

            if (!path.EndsWith(".axaml", StringComparison.OrdinalIgnoreCase))
                continue;

            var relativePath = path[ThemesPrefix.Length..];
            ThemeVariant? theme = null;

            if (relativePath.StartsWith(LightSubfolder, StringComparison.OrdinalIgnoreCase))
                theme = ThemeVariant.Light;
            else if (relativePath.StartsWith(DarkSubfolder, StringComparison.OrdinalIgnoreCase))
                theme = ThemeVariant.Dark;

            if (theme == null)
                continue;

            var uri = new Uri($"avares://{AssemblyName}/{path}");
            result.Add(new AvaloniaThemeResourceDescriptor(theme, uri));
        }

        return result;
    }

    /// <summary>
    /// Extracts Avalonia resource paths from assembly manifest resource names.
    /// Manifest names use '.' as separator; we convert back to '/' path format.
    /// </summary>
    private static IEnumerable<string> GetAvaloniaResourcePaths(Assembly assembly)
    {
        // Avalonia embeds resources with names like: Modulus.UI.Avalonia.Themes.Generic.axaml
        // We need to convert to path format: Themes/Generic.axaml
        var prefix = AssemblyName + ".";
        var names = assembly.GetManifestResourceNames()
            .Where(n => n.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) && n.EndsWith(".axaml", StringComparison.OrdinalIgnoreCase));

        foreach (var name in names)
        {
            // Remove assembly prefix and convert dots to slashes (except for file extension)
            var relativeName = name[prefix.Length..];
            
            // Find the last dot before .axaml (file extension boundary)
            var extIndex = relativeName.LastIndexOf(".axaml", StringComparison.OrdinalIgnoreCase);
            if (extIndex < 0) continue;

            var pathPart = relativeName[..extIndex].Replace('.', '/');
            yield return pathPart + ".axaml";
        }
    }
}

