using Avalonia;
using Avalonia.Controls;
using Modulus.Modules.ComponentsDemo.UI.Avalonia.Pages;

namespace Modulus.Modules.ComponentsDemo.UI.Avalonia;

/// <summary>
/// Control that hosts demo content based on the selected demo ID.
/// Uses XAML-based demo pages following control design guidelines.
/// </summary>
public partial class DemoContentControl : UserControl
{
    public static readonly StyledProperty<string> DemoIdProperty =
        AvaloniaProperty.Register<DemoContentControl, string>(nameof(DemoId), "basic-nav");

    public string DemoId
    {
        get => GetValue(DemoIdProperty);
        set => SetValue(DemoIdProperty, value);
    }

    private ContentControl? _contentHost;

    public DemoContentControl()
    {
        InitializeComponent();
        _contentHost = this.FindControl<ContentControl>("ContentHost");
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == DemoIdProperty)
        {
            UpdateContent();
        }
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        UpdateContent();
    }

    private void UpdateContent()
    {
        if (_contentHost == null) return;

        // Use XAML-based demo pages (following control design guidelines)
        _contentHost.Content = DemoId switch
        {
            "basic-nav" => new NavigationViewPage(),
            "badge-nav" => new BadgeDemoPage(),
            "disabled-nav" => new DisabledDemoPage(),
            "sub-item-1" or "sub-item-2" or "sub-item-3" or "sub-menu" => new SubMenuDemoPage(),
            "context-demo" => new ContextMenuDemoPage(),
            "lifecycle-demo" => new LifecycleDemoPage(),
            _ => new NavigationViewPage()
        };
    }
}
