using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using System.Windows.Input;

namespace Modulus.UI.Avalonia.Controls;

/// <summary>
/// A card control with frosted glass (acrylic) background effect.
/// </summary>
/// <remarks>
/// GlassCard provides a semi-transparent background with blur effect,
/// suitable for overlaying on colorful backgrounds.
/// </remarks>
public class GlassCard : Button
{
    #region Styled Properties

    /// <summary>
    /// Defines the <see cref="GlassOpacity"/> property.
    /// </summary>
    public static readonly StyledProperty<double> GlassOpacityProperty =
        AvaloniaProperty.Register<GlassCard, double>(nameof(GlassOpacity), defaultValue: 0.7);

    /// <summary>
    /// Defines the <see cref="GlassTint"/> property.
    /// </summary>
    public static readonly StyledProperty<Color> GlassTintProperty =
        AvaloniaProperty.Register<GlassCard, Color>(nameof(GlassTint), defaultValue: Color.Parse("#1E1E28"));

    /// <summary>
    /// Defines the <see cref="IsInteractive"/> property.
    /// </summary>
    public static readonly StyledProperty<bool> IsInteractiveProperty =
        AvaloniaProperty.Register<GlassCard, bool>(nameof(IsInteractive), defaultValue: true);

    #endregion

    #region CLR Properties

    /// <summary>
    /// Gets or sets the opacity of the glass effect (0-1).
    /// </summary>
    public double GlassOpacity
    {
        get => GetValue(GlassOpacityProperty);
        set => SetValue(GlassOpacityProperty, value);
    }

    /// <summary>
    /// Gets or sets the tint color of the glass effect.
    /// </summary>
    public Color GlassTint
    {
        get => GetValue(GlassTintProperty);
        set => SetValue(GlassTintProperty, value);
    }

    /// <summary>
    /// Gets or sets whether the card responds to hover/click.
    /// </summary>
    public bool IsInteractive
    {
        get => GetValue(IsInteractiveProperty);
        set => SetValue(IsInteractiveProperty, value);
    }

    #endregion

    static GlassCard()
    {
        IsInteractiveProperty.Changed.AddClassHandler<GlassCard>((x, _) => x.UpdatePseudoClasses());
    }

    public GlassCard()
    {
        UpdatePseudoClasses();
    }

    private void UpdatePseudoClasses()
    {
        PseudoClasses.Set(":interactive", IsInteractive);
    }
}

