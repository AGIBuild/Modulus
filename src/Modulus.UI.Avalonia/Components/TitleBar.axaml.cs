using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;

namespace Modulus.UI.Avalonia.Components;

/// <summary>
/// Title bar component with branding, navigation toggle, and customizable content.
/// </summary>
public partial class TitleBar : UserControl
{
    public static readonly StyledProperty<string> TitleProperty =
        AvaloniaProperty.Register<TitleBar, string>(nameof(Title), "MODULUS");

    public static readonly StyledProperty<string?> BadgeProperty =
        AvaloniaProperty.Register<TitleBar, string?>(nameof(Badge));

    public static readonly StyledProperty<ICommand?> ToggleCommandProperty =
        AvaloniaProperty.Register<TitleBar, ICommand?>(nameof(ToggleCommand));

    public static readonly StyledProperty<bool> ShowToggleProperty =
        AvaloniaProperty.Register<TitleBar, bool>(nameof(ShowToggle), true);

    public static readonly StyledProperty<object?> RightContentProperty =
        AvaloniaProperty.Register<TitleBar, object?>(nameof(RightContent));

    /// <summary>
    /// The title text displayed in the title bar.
    /// </summary>
    public string Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    /// <summary>
    /// Optional badge text displayed next to the title.
    /// </summary>
    public string? Badge
    {
        get => GetValue(BadgeProperty);
        set => SetValue(BadgeProperty, value);
    }

    /// <summary>
    /// Command executed when the toggle button is clicked.
    /// </summary>
    public ICommand? ToggleCommand
    {
        get => GetValue(ToggleCommandProperty);
        set => SetValue(ToggleCommandProperty, value);
    }

    /// <summary>
    /// Whether to show the navigation toggle button.
    /// </summary>
    public bool ShowToggle
    {
        get => GetValue(ShowToggleProperty);
        set => SetValue(ShowToggleProperty, value);
    }

    /// <summary>
    /// Content displayed on the right side of the title bar.
    /// </summary>
    public object? RightContent
    {
        get => GetValue(RightContentProperty);
        set => SetValue(RightContentProperty, value);
    }

    public TitleBar()
    {
        InitializeComponent();
    }
}

