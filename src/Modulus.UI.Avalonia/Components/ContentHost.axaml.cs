using Avalonia;
using Avalonia.Controls;

namespace Modulus.UI.Avalonia.Components;

/// <summary>
/// Content host component with customizable corner radius and padding.
/// </summary>
public partial class ContentHost : UserControl
{
    public static readonly StyledProperty<object?> HostedContentProperty =
        AvaloniaProperty.Register<ContentHost, object?>(nameof(HostedContent));

    public static readonly StyledProperty<CornerRadius> ContentCornerRadiusProperty =
        AvaloniaProperty.Register<ContentHost, CornerRadius>(nameof(ContentCornerRadius), new CornerRadius(12, 0, 0, 0));

    public static readonly StyledProperty<Thickness> ContentPaddingProperty =
        AvaloniaProperty.Register<ContentHost, Thickness>(nameof(ContentPadding), new Thickness(20));

    /// <summary>
    /// The content to be hosted.
    /// </summary>
    public object? HostedContent
    {
        get => GetValue(HostedContentProperty);
        set => SetValue(HostedContentProperty, value);
    }

    /// <summary>
    /// Corner radius of the content area.
    /// </summary>
    public CornerRadius ContentCornerRadius
    {
        get => GetValue(ContentCornerRadiusProperty);
        set => SetValue(ContentCornerRadiusProperty, value);
    }

    /// <summary>
    /// Padding of the content area.
    /// </summary>
    public Thickness ContentPadding
    {
        get => GetValue(ContentPaddingProperty);
        set => SetValue(ContentPaddingProperty, value);
    }

    public ContentHost()
    {
        InitializeComponent();
    }
}

