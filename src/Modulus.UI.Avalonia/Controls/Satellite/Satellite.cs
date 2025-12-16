using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using System.Windows.Input;

namespace Modulus.UI.Avalonia.Controls;

/// <summary>
/// A satellite card for the OrbitalSystem, representing a module or feature.
/// </summary>
/// <remarks>
/// Satellites display an icon and label, with an optional active state indicator.
/// </remarks>
public class Satellite : Button
{
    #region Styled Properties

    /// <summary>
    /// Defines the <see cref="Icon"/> property.
    /// </summary>
    public static readonly StyledProperty<string?> IconProperty =
        AvaloniaProperty.Register<Satellite, string?>(nameof(Icon));

    /// <summary>
    /// Defines the <see cref="Label"/> property.
    /// </summary>
    public static readonly StyledProperty<string?> LabelProperty =
        AvaloniaProperty.Register<Satellite, string?>(nameof(Label));

    /// <summary>
    /// Defines the <see cref="IsActive"/> property.
    /// </summary>
    public static readonly StyledProperty<bool> IsActiveProperty =
        AvaloniaProperty.Register<Satellite, bool>(nameof(IsActive));

    /// <summary>
    /// Defines the <see cref="ActiveColor"/> property.
    /// </summary>
    public static readonly StyledProperty<Color> ActiveColorProperty =
        AvaloniaProperty.Register<Satellite, Color>(nameof(ActiveColor), defaultValue: Color.Parse("#10b981"));

    /// <summary>
    /// Defines the <see cref="HoverGlowColor"/> property.
    /// </summary>
    public static readonly StyledProperty<Color> HoverGlowColorProperty =
        AvaloniaProperty.Register<Satellite, Color>(nameof(HoverGlowColor), defaultValue: Color.Parse("#6366f1"));

    #endregion

    #region CLR Properties

    /// <summary>
    /// Gets or sets the icon displayed on the satellite (emoji or icon key).
    /// </summary>
    public string? Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    /// <summary>
    /// Gets or sets the label text displayed on the satellite.
    /// </summary>
    public string? Label
    {
        get => GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    /// <summary>
    /// Gets or sets whether this satellite is active (shows status indicator).
    /// </summary>
    public bool IsActive
    {
        get => GetValue(IsActiveProperty);
        set => SetValue(IsActiveProperty, value);
    }

    /// <summary>
    /// Gets or sets the color of the active status indicator.
    /// </summary>
    public Color ActiveColor
    {
        get => GetValue(ActiveColorProperty);
        set => SetValue(ActiveColorProperty, value);
    }

    /// <summary>
    /// Gets or sets the glow color when hovering.
    /// </summary>
    public Color HoverGlowColor
    {
        get => GetValue(HoverGlowColorProperty);
        set => SetValue(HoverGlowColorProperty, value);
    }

    #endregion

    static Satellite()
    {
        IsActiveProperty.Changed.AddClassHandler<Satellite>((x, _) => x.UpdatePseudoClasses());
    }

    public Satellite()
    {
        UpdatePseudoClasses();
    }

    private void UpdatePseudoClasses()
    {
        PseudoClasses.Set(":active-satellite", IsActive);
    }
}

