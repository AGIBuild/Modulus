using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;

namespace Modulus.UI.Avalonia.Controls;

/// <summary>
/// A container that displays items in a uniform grid of cards.
/// </summary>
/// <remarks>
/// CardGrid ensures all child cards have consistent sizing and spacing.
/// </remarks>
public class CardGrid : TemplatedControl
{
    #region Styled Properties

    /// <summary>
    /// Defines the <see cref="ItemsSource"/> property.
    /// </summary>
    public static readonly StyledProperty<IEnumerable<object>?> ItemsSourceProperty =
        AvaloniaProperty.Register<CardGrid, IEnumerable<object>?>(nameof(ItemsSource));

    /// <summary>
    /// Defines the <see cref="ItemTemplate"/> property.
    /// </summary>
    public static readonly StyledProperty<IDataTemplate?> ItemTemplateProperty =
        AvaloniaProperty.Register<CardGrid, IDataTemplate?>(nameof(ItemTemplate));

    /// <summary>
    /// Defines the <see cref="CardMinWidth"/> property.
    /// </summary>
    public static readonly StyledProperty<double> CardMinWidthProperty =
        AvaloniaProperty.Register<CardGrid, double>(nameof(CardMinWidth), defaultValue: 200.0);

    /// <summary>
    /// Defines the <see cref="CardSpacing"/> property.
    /// </summary>
    public static readonly StyledProperty<double> CardSpacingProperty =
        AvaloniaProperty.Register<CardGrid, double>(nameof(CardSpacing), defaultValue: 16.0);

    /// <summary>
    /// Defines the <see cref="Columns"/> property.
    /// </summary>
    public static readonly StyledProperty<int> ColumnsProperty =
        AvaloniaProperty.Register<CardGrid, int>(nameof(Columns), defaultValue: 0);

    #endregion

    #region CLR Properties

    /// <summary>
    /// Gets or sets the items to display.
    /// </summary>
    public IEnumerable<object>? ItemsSource
    {
        get => GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    /// <summary>
    /// Gets or sets the template for each card.
    /// </summary>
    public IDataTemplate? ItemTemplate
    {
        get => GetValue(ItemTemplateProperty);
        set => SetValue(ItemTemplateProperty, value);
    }

    /// <summary>
    /// Gets or sets the minimum width for each card.
    /// </summary>
    public double CardMinWidth
    {
        get => GetValue(CardMinWidthProperty);
        set => SetValue(CardMinWidthProperty, value);
    }

    /// <summary>
    /// Gets or sets the spacing between cards.
    /// </summary>
    public double CardSpacing
    {
        get => GetValue(CardSpacingProperty);
        set => SetValue(CardSpacingProperty, value);
    }

    /// <summary>
    /// Gets or sets the number of columns (0 = auto).
    /// </summary>
    public int Columns
    {
        get => GetValue(ColumnsProperty);
        set => SetValue(ColumnsProperty, value);
    }

    #endregion
}

