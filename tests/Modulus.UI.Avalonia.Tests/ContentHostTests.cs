using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Modulus.UI.Avalonia.Controls;

namespace Modulus.UI.Avalonia.Tests;

/// <summary>
/// Tests for ContentHost control.
/// </summary>
public class ContentHostTests
{
    [AvaloniaFact]
    public void ContentHost_DefaultProperties()
    {
        var contentHost = new ContentHost();

        Assert.Null(contentHost.HostedContent);
    }

    [AvaloniaFact]
    public void ContentHost_HostedContent_CanBeSetToControl()
    {
        var contentHost = new ContentHost();
        var textBlock = new TextBlock { Text = "Test Content" };

        contentHost.HostedContent = textBlock;

        Assert.Equal(textBlock, contentHost.HostedContent);
    }

    [AvaloniaFact]
    public void ContentHost_HostedContent_CanBeSetToNull()
    {
        var contentHost = new ContentHost();
        var textBlock = new TextBlock { Text = "Test Content" };

        contentHost.HostedContent = textBlock;
        contentHost.HostedContent = null;

        Assert.Null(contentHost.HostedContent);
    }

    [AvaloniaFact]
    public void ContentHost_HostedContent_CanBeChanged()
    {
        var contentHost = new ContentHost();
        var content1 = new TextBlock { Text = "Content 1" };
        var content2 = new TextBlock { Text = "Content 2" };

        contentHost.HostedContent = content1;
        Assert.Equal(content1, contentHost.HostedContent);

        contentHost.HostedContent = content2;
        Assert.Equal(content2, contentHost.HostedContent);
    }

    [AvaloniaFact]
    public void ContentHost_HostedContent_CanBeUserControl()
    {
        var contentHost = new ContentHost();
        var userControl = new UserControl();

        contentHost.HostedContent = userControl;

        Assert.Equal(userControl, contentHost.HostedContent);
    }

    [AvaloniaFact]
    public void ContentHost_HostedContent_CanBePanel()
    {
        var contentHost = new ContentHost();
        var panel = new StackPanel();
        panel.Children.Add(new TextBlock { Text = "Item 1" });
        panel.Children.Add(new TextBlock { Text = "Item 2" });

        contentHost.HostedContent = panel;

        Assert.Equal(panel, contentHost.HostedContent);
    }
}

