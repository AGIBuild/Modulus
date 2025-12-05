using System.Collections.Generic;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Media;
using Avalonia.Styling;
using Modulus.UI.Abstractions;

namespace Modulus.UI.Avalonia.Icons;

/// <summary>
/// Helper class for resolving icons from IconKind enum to Geometry resources.
/// </summary>
public static class IconHelper
{
    private static readonly Dictionary<IconKind, string> IconResourceKeys = new()
    {
        [IconKind.None] = "Icon.None",
        [IconKind.Home] = "Icon.Home",
        [IconKind.Settings] = "Icon.Settings",
        [IconKind.Menu] = "Icon.Menu",
        [IconKind.ChevronRight] = "Icon.ChevronRight",
        [IconKind.ChevronDown] = "Icon.ChevronDown",
        [IconKind.ChevronLeft] = "Icon.ChevronLeft",
        [IconKind.ChevronUp] = "Icon.ChevronUp",
        [IconKind.ArrowLeft] = "Icon.ArrowLeft",
        [IconKind.ArrowRight] = "Icon.ArrowRight",
        [IconKind.Folder] = "Icon.Folder",
        [IconKind.FolderOpen] = "Icon.FolderOpen",
        [IconKind.File] = "Icon.File",
        [IconKind.Document] = "Icon.Document",
        [IconKind.Image] = "Icon.Image",
        [IconKind.Archive] = "Icon.Archive",
        [IconKind.Add] = "Icon.Add",
        [IconKind.Delete] = "Icon.Delete",
        [IconKind.Edit] = "Icon.Edit",
        [IconKind.Save] = "Icon.Save",
        [IconKind.Copy] = "Icon.Copy",
        [IconKind.Cut] = "Icon.Cut",
        [IconKind.Paste] = "Icon.Paste",
        [IconKind.Undo] = "Icon.Undo",
        [IconKind.Redo] = "Icon.Redo",
        [IconKind.Refresh] = "Icon.Refresh",
        [IconKind.Search] = "Icon.Search",
        [IconKind.Filter] = "Icon.Filter",
        [IconKind.Sort] = "Icon.Sort",
        [IconKind.Mail] = "Icon.Mail",
        [IconKind.Chat] = "Icon.Chat",
        [IconKind.Notification] = "Icon.Notification",
        [IconKind.Bell] = "Icon.Bell",
        [IconKind.Person] = "Icon.Person",
        [IconKind.People] = "Icon.People",
        [IconKind.Lock] = "Icon.Lock",
        [IconKind.Unlock] = "Icon.Unlock",
        [IconKind.Shield] = "Icon.Shield",
        [IconKind.Key] = "Icon.Key",
        [IconKind.Play] = "Icon.Play",
        [IconKind.Pause] = "Icon.Pause",
        [IconKind.Stop] = "Icon.Stop",
        [IconKind.Volume] = "Icon.Volume",
        [IconKind.VolumeMute] = "Icon.VolumeMute",
        [IconKind.Info] = "Icon.Info",
        [IconKind.Warning] = "Icon.Warning",
        [IconKind.Error] = "Icon.Error",
        [IconKind.Success] = "Icon.Success",
        [IconKind.Question] = "Icon.Question",
        [IconKind.Star] = "Icon.Star",
        [IconKind.Heart] = "Icon.Heart",
        [IconKind.Bookmark] = "Icon.Bookmark",
        [IconKind.Pin] = "Icon.Pin",
        [IconKind.Link] = "Icon.Link",
        [IconKind.Code] = "Icon.Code",
        [IconKind.Terminal] = "Icon.Terminal",
        [IconKind.Grid] = "Icon.Grid",
        [IconKind.List] = "Icon.List",
        [IconKind.Table] = "Icon.Table",
        [IconKind.Dashboard] = "Icon.Dashboard",
        [IconKind.Calendar] = "Icon.Calendar",
        [IconKind.Clock] = "Icon.Clock",
        [IconKind.Location] = "Icon.Location",
        [IconKind.Globe] = "Icon.Globe",
        [IconKind.Cloud] = "Icon.Cloud",
        [IconKind.Download] = "Icon.Download",
        [IconKind.Upload] = "Icon.Upload",
        [IconKind.Share] = "Icon.Share",
        [IconKind.Print] = "Icon.Print",
        [IconKind.Help] = "Icon.Help",
        [IconKind.MoreHorizontal] = "Icon.MoreHorizontal",
        [IconKind.MoreVertical] = "Icon.MoreVertical",
        [IconKind.Close] = "Icon.Close",
        [IconKind.Check] = "Icon.Check",
        [IconKind.Block] = "Icon.Block",
        [IconKind.Compass] = "Icon.Compass"
    };

    /// <summary>
    /// Gets the resource key for an IconKind.
    /// </summary>
    public static string GetResourceKey(IconKind iconKind)
    {
        return IconResourceKeys.TryGetValue(iconKind, out var key) ? key : "Icon.None";
    }

    /// <summary>
    /// Resolves an IconKind to a Geometry from application resources.
    /// Searches through Application.Resources, Styles, and merged dictionaries.
    /// </summary>
    public static Geometry? GetGeometry(IconKind iconKind)
    {
        if (iconKind == IconKind.None)
            return null;
            
        var key = GetResourceKey(iconKind);
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


