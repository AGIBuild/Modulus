using System.Collections.Generic;
using Avalonia.Data.Converters;
using Modulus.UI.Abstractions;

namespace Modulus.Modules.ComponentsDemo.UI.Avalonia;

public static class Converters
{
    public static readonly IValueConverter IsZero =
        new FuncValueConverter<int, bool>(count => count == 0);

    public static readonly IValueConverter IsNotZero =
        new FuncValueConverter<int, bool>(count => count > 0);

    public static readonly IValueConverter EnabledToOpacity =
        new FuncValueConverter<bool, double>(enabled => enabled ? 1.0 : 0.4);

    public static readonly IValueConverter ExpandedToChevron =
        new FuncValueConverter<bool, string>(expanded => expanded ? "▼" : "▶");

    /// <summary>
    /// Returns true if the collection is null or empty (show leaf item).
    /// </summary>
    public static readonly IValueConverter IsLeafItem =
        new FuncValueConverter<IReadOnlyList<MenuItem>?, bool>(children => 
            children == null || children.Count == 0);

    /// <summary>
    /// Returns true if the collection has items (show group header).
    /// </summary>
    public static readonly IValueConverter IsGroupItem =
        new FuncValueConverter<IReadOnlyList<MenuItem>?, bool>(children => 
            children != null && children.Count > 0);
}

