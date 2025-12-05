using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;

namespace Modulus.UI.Avalonia.Controls;

/// <summary>
/// A title bar control with branding, navigation toggle, and customizable right content.
/// </summary>
public class TitleBar : TemplatedControl
{
    protected override Type StyleKeyOverride => typeof(TitleBar);

    #region Styled Properties

    /// <summary>
    /// Defines the <see cref="Title"/> property.
    /// </summary>
    public static readonly StyledProperty<string> TitleProperty =
        AvaloniaProperty.Register<TitleBar, string>(nameof(Title), "MODULUS");

    /// <summary>
    /// Defines the <see cref="Badge"/> property.
    /// </summary>
    public static readonly StyledProperty<string?> BadgeProperty =
        AvaloniaProperty.Register<TitleBar, string?>(nameof(Badge));

    /// <summary>
    /// Defines the <see cref="ToggleCommand"/> property.
    /// </summary>
    public static readonly StyledProperty<ICommand?> ToggleCommandProperty =
        AvaloniaProperty.Register<TitleBar, ICommand?>(nameof(ToggleCommand));

    /// <summary>
    /// Defines the <see cref="ShowToggle"/> property.
    /// </summary>
    public static readonly StyledProperty<bool> ShowToggleProperty =
        AvaloniaProperty.Register<TitleBar, bool>(nameof(ShowToggle), true);

    /// <summary>
    /// Defines the <see cref="RightContent"/> property.
    /// </summary>
    public static readonly StyledProperty<object?> RightContentProperty =
        AvaloniaProperty.Register<TitleBar, object?>(nameof(RightContent));

    /// <summary>
    /// Defines the <see cref="Height"/> property.
    /// </summary>
    public static readonly new StyledProperty<double> HeightProperty =
        AvaloniaProperty.Register<TitleBar, double>(nameof(Height), 50);

    #endregion

    #region Properties

    /// <summary>
    /// Gets or sets the title text displayed in the title bar.
    /// </summary>
    public string Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    /// <summary>
    /// Gets or sets the optional badge text displayed next to the title.
    /// </summary>
    public string? Badge
    {
        get => GetValue(BadgeProperty);
        set => SetValue(BadgeProperty, value);
    }

    /// <summary>
    /// Gets or sets the command executed when the toggle button is clicked.
    /// </summary>
    public ICommand? ToggleCommand
    {
        get => GetValue(ToggleCommandProperty);
        set => SetValue(ToggleCommandProperty, value);
    }

    /// <summary>
    /// Gets or sets whether to show the navigation toggle button.
    /// </summary>
    public bool ShowToggle
    {
        get => GetValue(ShowToggleProperty);
        set => SetValue(ShowToggleProperty, value);
    }

    /// <summary>
    /// Gets or sets the content displayed on the right side of the title bar.
    /// </summary>
    public object? RightContent
    {
        get => GetValue(RightContentProperty);
        set => SetValue(RightContentProperty, value);
    }

    /// <summary>
    /// Gets or sets the height of the title bar.
    /// </summary>
    public new double Height
    {
        get => GetValue(HeightProperty);
        set => SetValue(HeightProperty, value);
    }

    #endregion
}

