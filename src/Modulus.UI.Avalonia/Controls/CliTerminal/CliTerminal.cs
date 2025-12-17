using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Interactivity;
using Avalonia.Media;
using System.Windows.Input;

namespace Modulus.UI.Avalonia.Controls;

/// <summary>
/// A terminal-style display for showing CLI commands.
/// </summary>
/// <remarks>
/// Mimics a terminal window with macOS-style window controls and command lines.
/// Supports copy-to-clipboard functionality for each command.
/// </remarks>
public class CliTerminal : TemplatedControl
{
    #region Styled Properties

    /// <summary>
    /// Defines the <see cref="Title"/> property.
    /// </summary>
    public static readonly StyledProperty<string?> TitleProperty =
        AvaloniaProperty.Register<CliTerminal, string?>(nameof(Title), defaultValue: "Terminal");

    /// <summary>
    /// Defines the <see cref="Commands"/> property.
    /// </summary>
    public static readonly StyledProperty<IEnumerable<object>?> CommandsProperty =
        AvaloniaProperty.Register<CliTerminal, IEnumerable<object>?>(nameof(Commands));

    /// <summary>
    /// Defines the <see cref="CommandTemplate"/> property.
    /// </summary>
    public static readonly StyledProperty<IDataTemplate?> CommandTemplateProperty =
        AvaloniaProperty.Register<CliTerminal, IDataTemplate?>(nameof(CommandTemplate));

    /// <summary>
    /// Defines the <see cref="PromptText"/> property.
    /// </summary>
    public static readonly StyledProperty<string> PromptTextProperty =
        AvaloniaProperty.Register<CliTerminal, string>(nameof(PromptText), defaultValue: "$");

    /// <summary>
    /// Defines the <see cref="PromptForeground"/> property.
    /// </summary>
    public static readonly StyledProperty<IBrush?> PromptForegroundProperty =
        AvaloniaProperty.Register<CliTerminal, IBrush?>(nameof(PromptForeground));

    /// <summary>
    /// Defines the <see cref="CopyCommand"/> property.
    /// </summary>
    public static readonly StyledProperty<ICommand?> CopyCommandProperty =
        AvaloniaProperty.Register<CliTerminal, ICommand?>(nameof(CopyCommand));

    #endregion

    #region CLR Properties

    /// <summary>
    /// Gets or sets the terminal window title.
    /// </summary>
    public string? Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    /// <summary>
    /// Gets or sets the collection of command items to display.
    /// </summary>
    public IEnumerable<object>? Commands
    {
        get => GetValue(CommandsProperty);
        set => SetValue(CommandsProperty, value);
    }

    /// <summary>
    /// Gets or sets the template for rendering command items.
    /// </summary>
    public IDataTemplate? CommandTemplate
    {
        get => GetValue(CommandTemplateProperty);
        set => SetValue(CommandTemplateProperty, value);
    }

    /// <summary>
    /// Gets or sets the prompt character/text (default: "$").
    /// </summary>
    public string PromptText
    {
        get => GetValue(PromptTextProperty);
        set => SetValue(PromptTextProperty, value);
    }

    /// <summary>
    /// Gets or sets the foreground brush for the prompt text.
    /// </summary>
    public IBrush? PromptForeground
    {
        get => GetValue(PromptForegroundProperty);
        set => SetValue(PromptForegroundProperty, value);
    }

    /// <summary>
    /// Gets or sets the command to execute when copying a command.
    /// </summary>
    public ICommand? CopyCommand
    {
        get => GetValue(CopyCommandProperty);
        set => SetValue(CopyCommandProperty, value);
    }

    #endregion
}

