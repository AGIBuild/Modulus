using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;

namespace Modulus.UI.Avalonia.Controls;

/// <summary>
/// A content host control with customizable corner radius and padding.
/// </summary>
public class ContentHost : TemplatedControl
{
    protected override Type StyleKeyOverride => typeof(ContentHost);

    #region Styled Properties

    /// <summary>
    /// Defines the <see cref="HostedContent"/> property.
    /// </summary>
    public static readonly StyledProperty<object?> HostedContentProperty =
        AvaloniaProperty.Register<ContentHost, object?>(nameof(HostedContent));

    /// <summary>
    /// Defines the <see cref="ContentCornerRadius"/> property.
    /// </summary>
    public static readonly StyledProperty<CornerRadius> ContentCornerRadiusProperty =
        AvaloniaProperty.Register<ContentHost, CornerRadius>(nameof(ContentCornerRadius), new CornerRadius(12, 0, 0, 0));

    /// <summary>
    /// Defines the <see cref="ContentPadding"/> property.
    /// </summary>
    public static readonly StyledProperty<Thickness> ContentPaddingProperty =
        AvaloniaProperty.Register<ContentHost, Thickness>(nameof(ContentPadding), new Thickness(20));

    #endregion

    #region Properties

    /// <summary>
    /// Gets or sets the content to be hosted.
    /// </summary>
    public object? HostedContent
    {
        get => GetValue(HostedContentProperty);
        set => SetValue(HostedContentProperty, value);
    }

    /// <summary>
    /// Gets or sets the corner radius of the content area.
    /// </summary>
    public CornerRadius ContentCornerRadius
    {
        get => GetValue(ContentCornerRadiusProperty);
        set => SetValue(ContentCornerRadiusProperty, value);
    }

    /// <summary>
    /// Gets or sets the padding of the content area.
    /// </summary>
    public Thickness ContentPadding
    {
        get => GetValue(ContentPaddingProperty);
        set => SetValue(ContentPaddingProperty, value);
    }

    #endregion
}

