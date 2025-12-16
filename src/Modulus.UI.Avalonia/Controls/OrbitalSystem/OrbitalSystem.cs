using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Media;

namespace Modulus.UI.Avalonia.Controls;

/// <summary>
/// A decorative orbital display showing a central core with static orbit rings.
/// </summary>
/// <remarks>
/// Used primarily for hero sections to visualize modular architecture.
/// Simplified version without animations or satellites.
/// </remarks>
public class OrbitalSystem : TemplatedControl
{
    #region Styled Properties

    /// <summary>
    /// Defines the <see cref="CoreContent"/> property.
    /// </summary>
    public static readonly StyledProperty<object?> CoreContentProperty =
        AvaloniaProperty.Register<OrbitalSystem, object?>(nameof(CoreContent));

    /// <summary>
    /// Defines the <see cref="CoreSize"/> property.
    /// </summary>
    public static readonly StyledProperty<double> CoreSizeProperty =
        AvaloniaProperty.Register<OrbitalSystem, double>(nameof(CoreSize), defaultValue: 100.0);

    /// <summary>
    /// Defines the <see cref="CoreBackground"/> property.
    /// </summary>
    public static readonly StyledProperty<IBrush?> CoreBackgroundProperty =
        AvaloniaProperty.Register<OrbitalSystem, IBrush?>(nameof(CoreBackground));

    /// <summary>
    /// Defines the <see cref="OrbitCount"/> property.
    /// </summary>
    public static readonly StyledProperty<int> OrbitCountProperty =
        AvaloniaProperty.Register<OrbitalSystem, int>(nameof(OrbitCount), defaultValue: 3);

    /// <summary>
    /// Defines the <see cref="Size"/> property for overall system size.
    /// </summary>
    public static readonly StyledProperty<double> SizeProperty =
        AvaloniaProperty.Register<OrbitalSystem, double>(nameof(Size), defaultValue: 300.0);

    #endregion

    #region CLR Properties

    /// <summary>
    /// Gets or sets the content displayed in the central core.
    /// </summary>
    public object? CoreContent
    {
        get => GetValue(CoreContentProperty);
        set => SetValue(CoreContentProperty, value);
    }

    /// <summary>
    /// Gets or sets the size of the central core (diameter).
    /// </summary>
    public double CoreSize
    {
        get => GetValue(CoreSizeProperty);
        set => SetValue(CoreSizeProperty, value);
    }

    /// <summary>
    /// Gets or sets the background brush for the central core.
    /// </summary>
    public IBrush? CoreBackground
    {
        get => GetValue(CoreBackgroundProperty);
        set => SetValue(CoreBackgroundProperty, value);
    }

    /// <summary>
    /// Gets or sets the number of orbital rings.
    /// </summary>
    public int OrbitCount
    {
        get => GetValue(OrbitCountProperty);
        set => SetValue(OrbitCountProperty, value);
    }

    /// <summary>
    /// Gets or sets the overall size of the orbital system.
    /// </summary>
    public double Size
    {
        get => GetValue(SizeProperty);
        set => SetValue(SizeProperty, value);
    }

    #endregion
}
