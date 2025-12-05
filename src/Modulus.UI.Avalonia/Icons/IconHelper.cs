using System;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Media;
using Avalonia.Styling;
using Modulus.UI.Abstractions;

namespace Modulus.UI.Avalonia.Icons;

/// <summary>
/// Helper class for resolving icons from IconKind enum to Geometry resources.
/// Supports Regular and Filled variants.
/// </summary>
public static class IconHelper
{
    private const string IconPrefix = "Icon.";

    /// <summary>
    /// Gets the resource key for an IconKind with optional variant suffix.
    /// Convention: Icon.{EnumName}.{Variant} (e.g., Icon.Home.Regular)
    /// </summary>
    public static string GetResourceKey(IconKind iconKind, IconVariant variant = IconVariant.Regular)
    {
        if (iconKind == IconKind.None)
            return $"{IconPrefix}None";
            
        var variantSuffix = variant switch
        {
            IconVariant.Filled => ".Filled",
            _ => ".Regular"
        };
        
        return $"{IconPrefix}{iconKind}{variantSuffix}";
    }

    /// <summary>
    /// Resolves an IconKind to a Geometry from application resources.
    /// Falls back to Regular variant if specified variant is not found.
    /// </summary>
    public static Geometry? GetGeometry(IconKind iconKind, IconVariant variant = IconVariant.Regular)
    {
        if (iconKind == IconKind.None)
            return null;
            
        var key = GetResourceKey(iconKind, variant);
        var geometry = FindGeometry(key);
        
        // Fallback to Regular variant if Filled not found
        if (geometry == null && variant == IconVariant.Filled)
        {
            key = GetResourceKey(iconKind, IconVariant.Regular);
            geometry = FindGeometry(key);
        }
        
        // Fallback to base key (no variant suffix) for backward compatibility
        if (geometry == null)
        {
            var baseKey = $"{IconPrefix}{iconKind}";
            geometry = FindGeometry(baseKey);
        }
        
        return geometry;
    }
    
    /// <summary>
    /// Resolves an IconKind to a Geometry (Regular variant).
    /// </summary>
    public static Geometry? GetGeometry(IconKind iconKind) => GetGeometry(iconKind, IconVariant.Regular);
    
    private static Geometry? FindGeometry(string key)
    {
        var app = Application.Current;
        if (app == null)
            return null;

        // Try direct resource lookup first (Application.Resources)
        if (app.Resources.TryGetResource(key, null, out var resource) && resource is Geometry geometry)
        {
            return geometry;
        }

        // Search in Styles' Resources (where Icons.axaml is merged via Generic.axaml)
        foreach (var style in app.Styles)
        {
            var result = TryFindInStyle(style, key);
            if (result != null)
                return result;
        }

        return null;
    }

    private static Geometry? TryFindInStyle(IStyle style, string key)
    {
        if (style is Styles styles)
        {
            // Check Resources directly
            if (styles.Resources is { } res)
            {
                if (res.TryGetResource(key, null, out var styleResource) && styleResource is Geometry styleGeometry)
                {
                    return styleGeometry;
                }
                
                // Check merged dictionaries
                if (res.MergedDictionaries != null)
                {
                    foreach (var merged in res.MergedDictionaries)
                    {
                        if (merged.TryGetResource(key, null, out var mergedResource) && mergedResource is Geometry mergedGeometry)
                        {
                            return mergedGeometry;
                        }
                    }
                }
            }

            // Recursively check nested styles
            foreach (var child in styles)
            {
                var result = TryFindInStyle(child, key);
                if (result != null)
                    return result;
            }
        }
        else if (style is StyleInclude include && include.Loaded is IStyle loadedStyle)
        {
            return TryFindInStyle(loadedStyle, key);
        }

        return null;
    }

    /// <summary>
    /// Converter that transforms IconKind to Geometry.
    /// </summary>
    public static readonly IValueConverter IconKindToGeometryConverter =
        new FuncValueConverter<IconKind, Geometry?>(GetGeometry);
}


