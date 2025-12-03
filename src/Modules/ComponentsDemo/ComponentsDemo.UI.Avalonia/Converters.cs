using Avalonia.Data.Converters;

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
}

