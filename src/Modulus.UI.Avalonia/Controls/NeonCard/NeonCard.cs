using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace Modulus.UI.Avalonia.Controls;

/// <summary>
/// A card control with neon glow hover effects and optional click navigation.
/// </summary>
/// <remarks>
/// NeonCard is implemented as a <see cref="Button"/> to ensure proper accessibility and input handling:
/// - Keyboard activation (Enter/Space)
/// - Focus visuals (theme-dependent)
/// - Built-in <see cref="Button.Command"/> and <see cref="Button.CommandParameter"/> support
/// The neon visuals and hover/float animations are provided by styles in XAML.
/// </remarks>
public partial class NeonCard : Button
{
    #region Constructor

    static NeonCard()
    {
        // Property change handlers
        IsHoverAnimationEnabledProperty.Changed.AddClassHandler<NeonCard>((x, _) => x.UpdatePseudoClasses());
        IsFloatAnimationEnabledProperty.Changed.AddClassHandler<NeonCard>((x, _) => x.UpdatePseudoClasses());

        GlowColorProperty.Changed.AddClassHandler<NeonCard>((x, _) => x.UpdateDerivedVisuals());
        GlowIntensityProperty.Changed.AddClassHandler<NeonCard>((x, _) => x.UpdateDerivedVisuals());
        FloatDistanceProperty.Changed.AddClassHandler<NeonCard>((x, _) => x.UpdateDerivedVisuals());
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NeonCard"/> class.
    /// </summary>
    public NeonCard()
    {
        UpdatePseudoClasses();
        UpdateDerivedVisuals();
    }

    #endregion

    #region Private Methods

    private void UpdatePseudoClasses()
    {
        PseudoClasses.Set(":hover-animation", IsHoverAnimationEnabled);
        PseudoClasses.Set(":float-animation", IsFloatAnimationEnabled);
    }

    private void UpdateDerivedVisuals()
    {
        // Border brush from glow color
        var glowBrush = new SolidColorBrush(GlowColor);
        SetAndRaise(GlowBorderBrushProperty, ref _glowBorderBrush, glowBrush);

        // Base shadow (always on)
        var normal = new BoxShadows(new BoxShadow
        {
            OffsetX = 0,
            OffsetY = 2,
            Blur = 8,
            Spread = 0,
            Color = Color.Parse("#20000000")
        });
        SetAndRaise(NormalBoxShadowsProperty, ref _normalBoxShadows, normal);

        // Hover shadow (base + glow)
        var glowAlpha = (byte)0x80;
        var glowColor = new Color(glowAlpha, GlowColor.R, GlowColor.G, GlowColor.B);
        var hoverBase = new BoxShadow
        {
            OffsetX = 0,
            OffsetY = 8,
            Blur = 24,
            Spread = 0,
            Color = Color.Parse("#30000000")
        };
        var hoverGlow = new BoxShadow
        {
            OffsetX = 0,
            OffsetY = 0,
            Blur = Math.Max(0, GlowIntensity),
            Spread = 2,
            Color = glowColor
        };
        var hover = new BoxShadows(hoverBase, new[] { hoverGlow });
        SetAndRaise(HoverBoxShadowsProperty, ref _hoverBoxShadows, hover);

        // Hover offset (negative)
        var offset = -Math.Abs(FloatDistance);
        SetAndRaise(HoverOffsetYProperty, ref _hoverOffsetY, offset);
    }

    #endregion
}


