using Avalonia;
using Avalonia.Media;

namespace Modulus.UI.Avalonia.Controls;

public partial class NeonCard
{
    #region Styled Properties

    /// <summary>
    /// Defines the <see cref="IsHoverAnimationEnabled"/> property.
    /// </summary>
    public static readonly StyledProperty<bool> IsHoverAnimationEnabledProperty =
        AvaloniaProperty.Register<NeonCard, bool>(nameof(IsHoverAnimationEnabled), defaultValue: true);

    /// <summary>
    /// Defines the <see cref="IsFloatAnimationEnabled"/> property.
    /// </summary>
    public static readonly StyledProperty<bool> IsFloatAnimationEnabledProperty =
        AvaloniaProperty.Register<NeonCard, bool>(nameof(IsFloatAnimationEnabled), defaultValue: true);

    /// <summary>
    /// Defines the <see cref="GlowColor"/> property.
    /// </summary>
    public static readonly StyledProperty<Color> GlowColorProperty =
        AvaloniaProperty.Register<NeonCard, Color>(nameof(GlowColor), defaultValue: Color.Parse("#BB8BFF"));

    /// <summary>
    /// Defines the <see cref="FloatDistance"/> property.
    /// </summary>
    public static readonly StyledProperty<double> FloatDistanceProperty =
        AvaloniaProperty.Register<NeonCard, double>(nameof(FloatDistance), defaultValue: 6.0);

    /// <summary>
    /// Defines the <see cref="GlowIntensity"/> property.
    /// </summary>
    public static readonly StyledProperty<double> GlowIntensityProperty =
        AvaloniaProperty.Register<NeonCard, double>(nameof(GlowIntensity), defaultValue: 20.0);

    #endregion

    #region Direct (Computed) Properties

    public static readonly DirectProperty<NeonCard, IBrush> GlowBorderBrushProperty =
        AvaloniaProperty.RegisterDirect<NeonCard, IBrush>(nameof(GlowBorderBrush), o => o.GlowBorderBrush);

    public static readonly DirectProperty<NeonCard, BoxShadows> NormalBoxShadowsProperty =
        AvaloniaProperty.RegisterDirect<NeonCard, BoxShadows>(nameof(NormalBoxShadows), o => o.NormalBoxShadows);

    public static readonly DirectProperty<NeonCard, BoxShadows> HoverBoxShadowsProperty =
        AvaloniaProperty.RegisterDirect<NeonCard, BoxShadows>(nameof(HoverBoxShadows), o => o.HoverBoxShadows);

    public static readonly DirectProperty<NeonCard, double> HoverOffsetYProperty =
        AvaloniaProperty.RegisterDirect<NeonCard, double>(nameof(HoverOffsetY), o => o.HoverOffsetY);

    #endregion

    #region CLR Properties

    /// <summary>
    /// Gets or sets whether the hover glow animation is enabled.
    /// </summary>
    public bool IsHoverAnimationEnabled
    {
        get => GetValue(IsHoverAnimationEnabledProperty);
        set => SetValue(IsHoverAnimationEnabledProperty, value);
    }

    /// <summary>
    /// Gets or sets whether the float (rise) animation is enabled on hover.
    /// </summary>
    public bool IsFloatAnimationEnabled
    {
        get => GetValue(IsFloatAnimationEnabledProperty);
        set => SetValue(IsFloatAnimationEnabledProperty, value);
    }

    /// <summary>
    /// Gets or sets the glow color for the neon effect.
    /// </summary>
    public Color GlowColor
    {
        get => GetValue(GlowColorProperty);
        set => SetValue(GlowColorProperty, value);
    }

    /// <summary>
    /// Gets or sets the distance the card floats up on hover (in pixels).
    /// </summary>
    public double FloatDistance
    {
        get => GetValue(FloatDistanceProperty);
        set => SetValue(FloatDistanceProperty, value);
    }

    /// <summary>
    /// Gets or sets the intensity of the glow effect (blur radius in pixels).
    /// </summary>
    public double GlowIntensity
    {
        get => GetValue(GlowIntensityProperty);
        set => SetValue(GlowIntensityProperty, value);
    }

    /// <summary>
    /// Gets the glow border brush derived from <see cref="GlowColor"/>.
    /// </summary>
    public IBrush GlowBorderBrush => _glowBorderBrush;

    /// <summary>
    /// Gets the normal box shadows for the card.
    /// </summary>
    public BoxShadows NormalBoxShadows => _normalBoxShadows;

    /// <summary>
    /// Gets the hover box shadows for the card (including glow).
    /// </summary>
    public BoxShadows HoverBoxShadows => _hoverBoxShadows;

    /// <summary>
    /// Gets the hover vertical offset (negative value) derived from <see cref="FloatDistance"/>.
    /// </summary>
    public double HoverOffsetY => _hoverOffsetY;

    #endregion

    #region Backing Fields (Direct Properties)

    private IBrush _glowBorderBrush = Brushes.Transparent;
    private BoxShadows _normalBoxShadows = default;
    private BoxShadows _hoverBoxShadows = default;
    private double _hoverOffsetY;

    #endregion
}


