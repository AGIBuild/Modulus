using Modulus.UI.Abstractions;

namespace Modulus.Hosts.Tests;

/// <summary>
/// Tests for IconKind enum.
/// </summary>
public class IconKindTests
{
    [Fact]
    public void IconKind_None_IsZero()
    {
        Assert.Equal(0, (int)IconKind.None);
    }

    [Theory]
    [InlineData(IconKind.Home)]
    [InlineData(IconKind.Settings)]
    [InlineData(IconKind.Terminal)]
    [InlineData(IconKind.Grid)]
    [InlineData(IconKind.Folder)]
    [InlineData(IconKind.File)]
    public void IconKind_CommonIcons_AreDefined(IconKind icon)
    {
        Assert.True(Enum.IsDefined(typeof(IconKind), icon));
    }

    [Fact]
    public void IconKind_NavigationIcons_AreDefined()
    {
        var navigationIcons = new[]
        {
            IconKind.Home,
            IconKind.Settings,
            IconKind.Menu,
            IconKind.ChevronRight,
            IconKind.ChevronDown,
            IconKind.ChevronLeft,
            IconKind.ChevronUp,
            IconKind.ArrowLeft,
            IconKind.ArrowRight,
            IconKind.ArrowUp,
            IconKind.ArrowDown,
            IconKind.Back,
            IconKind.Forward,
            IconKind.History
        };

        foreach (var icon in navigationIcons)
        {
            Assert.True(Enum.IsDefined(typeof(IconKind), icon), $"Icon {icon} should be defined");
        }
    }

    [Fact]
    public void IconKind_FileIcons_AreDefined()
    {
        var fileIcons = new[]
        {
            IconKind.Folder,
            IconKind.FolderOpen,
            IconKind.File,
            IconKind.Document,
            IconKind.Image,
            IconKind.Archive,
            IconKind.Attachment
        };

        foreach (var icon in fileIcons)
        {
            Assert.True(Enum.IsDefined(typeof(IconKind), icon), $"Icon {icon} should be defined");
        }
    }

    [Fact]
    public void IconKind_ActionIcons_AreDefined()
    {
        var actionIcons = new[]
        {
            IconKind.Add,
            IconKind.Delete,
            IconKind.Edit,
            IconKind.Save,
            IconKind.Copy,
            IconKind.Cut,
            IconKind.Paste,
            IconKind.Undo,
            IconKind.Redo,
            IconKind.Refresh,
            IconKind.Search,
            IconKind.Filter,
            IconKind.Sort,
            IconKind.SelectAll,
            IconKind.Clear
        };

        foreach (var icon in actionIcons)
        {
            Assert.True(Enum.IsDefined(typeof(IconKind), icon), $"Icon {icon} should be defined");
        }
    }

    [Fact]
    public void IconKind_StatusIcons_AreDefined()
    {
        var statusIcons = new[]
        {
            IconKind.Info,
            IconKind.Warning,
            IconKind.Error,
            IconKind.Success,
            IconKind.Question,
            IconKind.Loading,
            IconKind.Sync,
            IconKind.Online,
            IconKind.Offline
        };

        foreach (var icon in statusIcons)
        {
            Assert.True(Enum.IsDefined(typeof(IconKind), icon), $"Icon {icon} should be defined");
        }
    }

    [Fact]
    public void IconKind_DevelopmentIcons_AreDefined()
    {
        var devIcons = new[]
        {
            IconKind.Bug,
            IconKind.Database,
            IconKind.Server,
            IconKind.Api,
            IconKind.Plugin,
            IconKind.Extension,
            IconKind.Branch,
            IconKind.Merge,
            IconKind.Code,
            IconKind.Terminal
        };

        foreach (var icon in devIcons)
        {
            Assert.True(Enum.IsDefined(typeof(IconKind), icon), $"Icon {icon} should be defined");
        }
    }

    [Fact]
    public void IconKind_ViewIcons_AreDefined()
    {
        var viewIcons = new[]
        {
            IconKind.Eye,
            IconKind.EyeOff,
            IconKind.ZoomIn,
            IconKind.ZoomOut,
            IconKind.Fullscreen,
            IconKind.ExitFullscreen,
            IconKind.Expand,
            IconKind.Collapse
        };

        foreach (var icon in viewIcons)
        {
            Assert.True(Enum.IsDefined(typeof(IconKind), icon), $"Icon {icon} should be defined");
        }
    }

    [Fact]
    public void IconKind_WindowIcons_AreDefined()
    {
        var windowIcons = new[]
        {
            IconKind.Minimize,
            IconKind.Maximize,
            IconKind.Restore,
            IconKind.Window,
            IconKind.Close
        };

        foreach (var icon in windowIcons)
        {
            Assert.True(Enum.IsDefined(typeof(IconKind), icon), $"Icon {icon} should be defined");
        }
    }

    [Fact]
    public void IconKind_ChartIcons_AreDefined()
    {
        var chartIcons = new[]
        {
            IconKind.Chart,
            IconKind.PieChart,
            IconKind.BarChart,
            IconKind.LineChart,
            IconKind.Analytics,
            IconKind.Dashboard
        };

        foreach (var icon in chartIcons)
        {
            Assert.True(Enum.IsDefined(typeof(IconKind), icon), $"Icon {icon} should be defined");
        }
    }

    [Fact]
    public void IconKind_HardwareIcons_AreDefined()
    {
        var hardwareIcons = new[]
        {
            IconKind.Cpu,
            IconKind.Memory,
            IconKind.Disk,
            IconKind.Network,
            IconKind.Wifi,
            IconKind.WifiOff,
            IconKind.Bluetooth,
            IconKind.Usb,
            IconKind.Battery,
            IconKind.Power
        };

        foreach (var icon in hardwareIcons)
        {
            Assert.True(Enum.IsDefined(typeof(IconKind), icon), $"Icon {icon} should be defined");
        }
    }

    [Fact]
    public void IconKind_TextFormattingIcons_AreDefined()
    {
        var textIcons = new[]
        {
            IconKind.Bold,
            IconKind.Italic,
            IconKind.Underline,
            IconKind.Strikethrough,
            IconKind.TextFormat,
            IconKind.AlignLeft,
            IconKind.AlignCenter,
            IconKind.AlignRight
        };

        foreach (var icon in textIcons)
        {
            Assert.True(Enum.IsDefined(typeof(IconKind), icon), $"Icon {icon} should be defined");
        }
    }

    [Fact]
    public void IconKind_EcommerceIcons_AreDefined()
    {
        var ecommerceIcons = new[]
        {
            IconKind.Cart,
            IconKind.Payment,
            IconKind.Receipt,
            IconKind.Wallet,
            IconKind.CreditCard
        };

        foreach (var icon in ecommerceIcons)
        {
            Assert.True(Enum.IsDefined(typeof(IconKind), icon), $"Icon {icon} should be defined");
        }
    }

    [Fact]
    public void IconKind_ToString_ReturnsEnumName()
    {
        Assert.Equal("Home", IconKind.Home.ToString());
        Assert.Equal("Settings", IconKind.Settings.ToString());
        Assert.Equal("Terminal", IconKind.Terminal.ToString());
    }

    [Fact]
    public void IconKind_Parse_FromString()
    {
        Assert.Equal(IconKind.Home, Enum.Parse<IconKind>("Home"));
        Assert.Equal(IconKind.Settings, Enum.Parse<IconKind>("Settings"));
        Assert.Equal(IconKind.Terminal, Enum.Parse<IconKind>("Terminal"));
    }

    [Fact]
    public void IconKind_TotalCount_IsReasonable()
    {
        var iconCount = Enum.GetValues<IconKind>().Length;
        // We have about 150+ icons defined
        Assert.True(iconCount >= 100, $"Expected at least 100 icons, got {iconCount}");
    }
}

/// <summary>
/// Tests for IconVariant enum.
/// </summary>
public class IconVariantTests
{
    [Fact]
    public void IconVariant_Regular_IsZero()
    {
        Assert.Equal(0, (int)IconVariant.Regular);
    }

    [Fact]
    public void IconVariant_Filled_IsOne()
    {
        Assert.Equal(1, (int)IconVariant.Filled);
    }

    [Fact]
    public void IconVariant_HasTwoValues()
    {
        var values = Enum.GetValues<IconVariant>();
        Assert.Equal(2, values.Length);
    }

    [Fact]
    public void IconVariant_ToString_ReturnsEnumName()
    {
        Assert.Equal("Regular", IconVariant.Regular.ToString());
        Assert.Equal("Filled", IconVariant.Filled.ToString());
    }

    [Fact]
    public void IconVariant_Parse_FromString()
    {
        Assert.Equal(IconVariant.Regular, Enum.Parse<IconVariant>("Regular"));
        Assert.Equal(IconVariant.Filled, Enum.Parse<IconVariant>("Filled"));
    }

    [Fact]
    public void IconVariant_Default_IsRegular()
    {
        IconVariant defaultVariant = default;
        Assert.Equal(IconVariant.Regular, defaultVariant);
    }
}

