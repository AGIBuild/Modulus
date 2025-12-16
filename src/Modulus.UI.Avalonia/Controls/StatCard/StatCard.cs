using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Media;

namespace Modulus.UI.Avalonia.Controls;

/// <summary>
/// A compact card for displaying a statistic value with a label.
/// </summary>
/// <remarks>
/// Used in dashboards and home pages to show key metrics like module counts.
/// </remarks>
public class StatCard : TemplatedControl
{
    #region Styled Properties

    /// <summary>
    /// Defines the <see cref="Value"/> property.
    /// </summary>
    public static readonly StyledProperty<string?> ValueProperty =
        AvaloniaProperty.Register<StatCard, string?>(nameof(Value));

    /// <summary>
    /// Defines the <see cref="Label"/> property.
    /// </summary>
    public static readonly StyledProperty<string?> LabelProperty =
        AvaloniaProperty.Register<StatCard, string?>(nameof(Label));

    /// <summary>
    /// Defines the <see cref="ValueForeground"/> property.
    /// </summary>
    public static readonly StyledProperty<IBrush?> ValueForegroundProperty =
        AvaloniaProperty.Register<StatCard, IBrush?>(nameof(ValueForeground));

    /// <summary>
    /// Defines the <see cref="ValueFontSize"/> property.
    /// </summary>
    public static readonly StyledProperty<double> ValueFontSizeProperty =
        AvaloniaProperty.Register<StatCard, double>(nameof(ValueFontSize), defaultValue: 28.0);

    /// <summary>
    /// Defines the <see cref="LabelFontSize"/> property.
    /// </summary>
    public static readonly StyledProperty<double> LabelFontSizeProperty =
        AvaloniaProperty.Register<StatCard, double>(nameof(LabelFontSize), defaultValue: 12.0);

    #endregion

    #region CLR Properties

    /// <summary>
    /// Gets or sets the statistic value to display.
    /// </summary>
    public string? Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    /// <summary>
    /// Gets or sets the label describing the statistic.
    /// </summary>
    public string? Label
    {
        get => GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    /// <summary>
    /// Gets or sets the foreground brush for the value text.
    /// </summary>
    public IBrush? ValueForeground
    {
        get => GetValue(ValueForegroundProperty);
        set => SetValue(ValueForegroundProperty, value);
    }

    /// <summary>
    /// Gets or sets the font size for the value text.
    /// </summary>
    public double ValueFontSize
    {
        get => GetValue(ValueFontSizeProperty);
        set => SetValue(ValueFontSizeProperty, value);
    }

    /// <summary>
    /// Gets or sets the font size for the label text.
    /// </summary>
    public double LabelFontSize
    {
        get => GetValue(LabelFontSizeProperty);
        set => SetValue(LabelFontSizeProperty, value);
    }

    #endregion
}

