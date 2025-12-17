using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Styling;

namespace Modulus.UI.Avalonia.Controls;

/// <summary>
/// A themed message dialog window for displaying messages and confirmations.
/// Automatically adapts to the current application theme (Light/Dark).
/// </summary>
public class MessageDialog : Window
{
    private readonly MessageDialogType _dialogType;
    private readonly string _message;
    private readonly string _primaryButtonText;
    private readonly string _secondaryButtonText;
    private readonly bool _showSecondaryButton;

    /// <summary>
    /// The result of the dialog (true if primary button clicked).
    /// </summary>
    public bool DialogResult { get; private set; }

    private MessageDialog(
        string title, 
        string message, 
        MessageDialogType dialogType,
        string primaryButtonText = "OK",
        string? secondaryButtonText = null)
    {
        _message = message;
        _dialogType = dialogType;
        _primaryButtonText = primaryButtonText;
        _secondaryButtonText = secondaryButtonText ?? "Cancel";
        _showSecondaryButton = secondaryButtonText != null;

        Title = title;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        CanResize = false;
        ShowInTaskbar = false;
        SizeToContent = SizeToContent.Height;
        Width = 420;
        MinHeight = 150;
        MaxHeight = 400;
        
        // Apply theme-aware styling
        ExtendClientAreaToDecorationsHint = true;
        ExtendClientAreaChromeHints = ExtendClientAreaChromeHints.NoChrome;
        ExtendClientAreaTitleBarHeightHint = -1;

        BuildContent();
    }

    private bool IsDarkTheme
    {
        get
        {
            var theme = Application.Current?.ActualThemeVariant;
            return theme == ThemeVariant.Dark;
        }
    }

    // Theme-aware colors
    private Color BackgroundColor => IsDarkTheme ? Color.Parse("#1E1E1E") : Color.Parse("#FFFFFF");
    private Color TitleBarColor => IsDarkTheme ? Color.Parse("#1A1A1A") : Color.Parse("#F5F5F5");
    private Color TitleTextColor => IsDarkTheme ? Color.Parse("#E8E8E8") : Color.Parse("#1A1A1A");
    private Color MessageTextColor => IsDarkTheme ? Color.Parse("#A0A0A0") : Color.Parse("#666666");
    private Color ButtonBackgroundColor => IsDarkTheme ? Color.Parse("#2D2D2D") : Color.Parse("#E8E8E8");
    private Color ButtonTextColor => IsDarkTheme ? Color.Parse("#E8E8E8") : Color.Parse("#1A1A1A");
    private Color ButtonBorderColor => IsDarkTheme ? Color.Parse("#404040") : Color.Parse("#CCCCCC");
    private Color PrimaryButtonBackgroundColor => IsDarkTheme ? Color.Parse("#1E1E1E") : Color.Parse("#F0F0F0");

    private Color GetIconColor()
    {
        return _dialogType switch
        {
            MessageDialogType.Info => Color.Parse("#3B82F6"),
            MessageDialogType.Warning => Color.Parse("#F59E0B"),
            MessageDialogType.Error => Color.Parse("#EF4444"),
            MessageDialogType.Confirm => Color.Parse("#6366F1"),
            _ => Color.Parse("#3B82F6")
        };
    }

    private string GetIconText()
    {
        return _dialogType switch
        {
            MessageDialogType.Info => "ℹ",
            MessageDialogType.Warning => "⚠",
            MessageDialogType.Error => "✕",
            MessageDialogType.Confirm => "?",
            _ => "ℹ"
        };
    }

    private void BuildContent()
    {
        // Main container
        var mainPanel = new DockPanel();

        // Title bar
        var titleBar = new Border
        {
            Background = new SolidColorBrush(TitleBarColor),
            Padding = new Thickness(16, 12),
            CornerRadius = new CornerRadius(8, 8, 0, 0)
        };

        var titleGrid = new Grid
        {
            ColumnDefinitions = ColumnDefinitions.Parse("Auto,*")
        };

        // Icon
        var iconBlock = new TextBlock
        {
            Text = GetIconText(),
            FontSize = 20,
            FontWeight = FontWeight.Bold,
            Foreground = new SolidColorBrush(GetIconColor()),
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 12, 0)
        };
        Grid.SetColumn(iconBlock, 0);

        // Title text
        var titleText = new TextBlock
        {
            Text = Title,
            FontWeight = FontWeight.SemiBold,
            FontSize = 14,
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = new SolidColorBrush(TitleTextColor)
        };
        Grid.SetColumn(titleText, 1);

        titleGrid.Children.Add(iconBlock);
        titleGrid.Children.Add(titleText);
        titleBar.Child = titleGrid;
        DockPanel.SetDock(titleBar, Dock.Top);
        mainPanel.Children.Add(titleBar);

        // Content area
        var contentBorder = new Border
        {
            Padding = new Thickness(24),
            Background = new SolidColorBrush(BackgroundColor)
        };

        var contentPanel = new DockPanel();

        // Buttons
        var buttonPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = 8,
            Margin = new Thickness(0, 20, 0, 0)
        };

        if (_showSecondaryButton)
        {
            var secondaryButton = new Button
            {
                Content = _secondaryButtonText,
                Padding = new Thickness(20, 8),
                MinWidth = 80,
                Background = new SolidColorBrush(ButtonBackgroundColor),
                Foreground = new SolidColorBrush(ButtonTextColor),
                BorderThickness = new Thickness(1),
                BorderBrush = new SolidColorBrush(ButtonBorderColor)
            };
            secondaryButton.Click += (_, _) => { DialogResult = false; Close(); };
            buttonPanel.Children.Add(secondaryButton);
        }

        var primaryButton = new Button
        {
            Content = _primaryButtonText,
            Padding = new Thickness(20, 8),
            MinWidth = 80,
            Background = new SolidColorBrush(PrimaryButtonBackgroundColor),
            Foreground = new SolidColorBrush(ButtonTextColor),
            BorderThickness = new Thickness(0)
        };
        primaryButton.Click += (_, _) => { DialogResult = true; Close(); };
        buttonPanel.Children.Add(primaryButton);

        DockPanel.SetDock(buttonPanel, Dock.Bottom);
        contentPanel.Children.Add(buttonPanel);

        // Message
        // Use read-only TextBox so multi-line diagnostics render reliably and users can copy details.
        var messageBox = new TextBox
        {
            Text = _message.Replace("\r\n", "\n"),
            IsReadOnly = true,
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            FontSize = 14,
            Foreground = new SolidColorBrush(MessageTextColor),
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            MinHeight = 60
        };

        var messageScroll = new ScrollViewer
        {
            MaxHeight = 200,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Content = messageBox
        };

        contentPanel.Children.Add(messageScroll);

        contentBorder.Child = contentPanel;
        mainPanel.Children.Add(contentBorder);

        // Window styling
        Background = new SolidColorBrush(BackgroundColor);
        
        Content = mainPanel;
    }

    /// <summary>
    /// Shows an informational message dialog.
    /// </summary>
    public static async Task ShowInfoAsync(Window owner, string title, string message)
    {
        var dialog = new MessageDialog(title, message, MessageDialogType.Info);
        await dialog.ShowDialog(owner);
    }

    /// <summary>
    /// Shows a warning message dialog.
    /// </summary>
    public static async Task ShowWarningAsync(Window owner, string title, string message)
    {
        var dialog = new MessageDialog(title, message, MessageDialogType.Warning);
        await dialog.ShowDialog(owner);
    }

    /// <summary>
    /// Shows an error message dialog.
    /// </summary>
    public static async Task ShowErrorAsync(Window owner, string title, string message)
    {
        var dialog = new MessageDialog(title, message, MessageDialogType.Error);
        await dialog.ShowDialog(owner);
    }

    /// <summary>
    /// Shows a confirmation dialog and returns the user's choice.
    /// </summary>
    public static async Task<bool> ConfirmAsync(Window owner, string title, string message, 
        string confirmText = "Yes", string cancelText = "No")
    {
        var dialog = new MessageDialog(title, message, MessageDialogType.Confirm, confirmText, cancelText);
        await dialog.ShowDialog(owner);
        return dialog.DialogResult;
    }
}
