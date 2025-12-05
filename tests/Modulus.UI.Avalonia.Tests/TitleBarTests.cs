using Avalonia.Headless.XUnit;
using Modulus.UI.Avalonia.Controls;

namespace Modulus.UI.Avalonia.Tests;

/// <summary>
/// Tests for TitleBar control.
/// </summary>
public class TitleBarTests
{
    [AvaloniaFact]
    public void TitleBar_DefaultProperties()
    {
        var titleBar = new TitleBar();

        // Default values from TitleBar control
        Assert.Equal("MODULUS", titleBar.Title);
        Assert.Null(titleBar.Badge);
        Assert.Null(titleBar.ToggleCommand);
        Assert.True(titleBar.ShowToggle); // Default is true
        Assert.Equal(50, titleBar.Height); // Default height
    }

    [AvaloniaFact]
    public void TitleBar_Title_CanBeSet()
    {
        var titleBar = new TitleBar();

        titleBar.Title = "My Application";

        Assert.Equal("My Application", titleBar.Title);
    }

    [AvaloniaFact]
    public void TitleBar_Badge_CanBeSet()
    {
        var titleBar = new TitleBar();

        titleBar.Badge = "BETA";

        Assert.Equal("BETA", titleBar.Badge);
    }

    [AvaloniaFact]
    public void TitleBar_ShowToggle_CanBeToggled()
    {
        var titleBar = new TitleBar();

        Assert.True(titleBar.ShowToggle); // Default is true
        titleBar.ShowToggle = false;
        Assert.False(titleBar.ShowToggle);
        titleBar.ShowToggle = true;
        Assert.True(titleBar.ShowToggle);
    }

    [AvaloniaFact]
    public void TitleBar_Title_CanBeEmpty()
    {
        var titleBar = new TitleBar();

        titleBar.Title = "";

        Assert.Equal("", titleBar.Title);
    }

    [AvaloniaFact]
    public void TitleBar_Badge_CanBeNull()
    {
        var titleBar = new TitleBar();
        titleBar.Badge = "TEST";

        titleBar.Badge = null;

        Assert.Null(titleBar.Badge);
    }

    [AvaloniaFact]
    public void TitleBar_AllProperties_CanBeSetTogether()
    {
        var titleBar = new TitleBar
        {
            Title = "Test App",
            Badge = "DEV",
            ShowToggle = true
        };

        Assert.Equal("Test App", titleBar.Title);
        Assert.Equal("DEV", titleBar.Badge);
        Assert.True(titleBar.ShowToggle);
    }
}

