using Modulus.UI.Abstractions;
using Modulus.UI.Avalonia.Icons;

namespace Modulus.UI.Avalonia.Tests;

/// <summary>
/// Tests for IconHelper class.
/// </summary>
public class IconHelperTests
{
    #region GetResourceKey Tests

    [Fact]
    public void GetResourceKey_None_ReturnsIconNone()
    {
        var result = IconHelper.GetResourceKey(IconKind.None);
        Assert.Equal("Icon.None", result);
    }

    [Fact]
    public void GetResourceKey_Home_Regular_ReturnsCorrectKey()
    {
        var result = IconHelper.GetResourceKey(IconKind.Home, IconVariant.Regular);
        Assert.Equal("Icon.Home.Regular", result);
    }

    [Fact]
    public void GetResourceKey_Home_Filled_ReturnsCorrectKey()
    {
        var result = IconHelper.GetResourceKey(IconKind.Home, IconVariant.Filled);
        Assert.Equal("Icon.Home.Filled", result);
    }

    [Fact]
    public void GetResourceKey_DefaultVariant_IsRegular()
    {
        var result = IconHelper.GetResourceKey(IconKind.Settings);
        Assert.Equal("Icon.Settings.Regular", result);
    }

    [Theory]
    [InlineData(IconKind.Home, IconVariant.Regular, "Icon.Home.Regular")]
    [InlineData(IconKind.Home, IconVariant.Filled, "Icon.Home.Filled")]
    [InlineData(IconKind.Settings, IconVariant.Regular, "Icon.Settings.Regular")]
    [InlineData(IconKind.Settings, IconVariant.Filled, "Icon.Settings.Filled")]
    [InlineData(IconKind.Terminal, IconVariant.Regular, "Icon.Terminal.Regular")]
    [InlineData(IconKind.Grid, IconVariant.Filled, "Icon.Grid.Filled")]
    [InlineData(IconKind.Folder, IconVariant.Regular, "Icon.Folder.Regular")]
    [InlineData(IconKind.Delete, IconVariant.Filled, "Icon.Delete.Filled")]
    public void GetResourceKey_VariousIcons_ReturnsCorrectKey(IconKind icon, IconVariant variant, string expected)
    {
        var result = IconHelper.GetResourceKey(icon, variant);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void GetResourceKey_AllIcons_FollowNamingConvention()
    {
        foreach (var iconKind in Enum.GetValues<IconKind>())
        {
            if (iconKind == IconKind.None) continue;

            var regularKey = IconHelper.GetResourceKey(iconKind, IconVariant.Regular);
            var filledKey = IconHelper.GetResourceKey(iconKind, IconVariant.Filled);

            Assert.StartsWith("Icon.", regularKey);
            Assert.EndsWith(".Regular", regularKey);
            Assert.StartsWith("Icon.", filledKey);
            Assert.EndsWith(".Filled", filledKey);

            // Verify the icon name is in the key
            Assert.Contains(iconKind.ToString(), regularKey);
            Assert.Contains(iconKind.ToString(), filledKey);
        }
    }

    [Fact]
    public void GetResourceKey_None_IgnoresVariant()
    {
        var regularResult = IconHelper.GetResourceKey(IconKind.None, IconVariant.Regular);
        var filledResult = IconHelper.GetResourceKey(IconKind.None, IconVariant.Filled);
        
        // Both should return "Icon.None" regardless of variant
        Assert.Equal("Icon.None", regularResult);
        Assert.Equal("Icon.None", filledResult);
    }

    #endregion

    #region GetResourceKey Naming Convention Tests

    [Fact]
    public void GetResourceKey_NavigationIcons_CorrectFormat()
    {
        var icons = new[] { IconKind.Home, IconKind.Settings, IconKind.Menu, IconKind.Back, IconKind.Forward };
        
        foreach (var icon in icons)
        {
            var key = IconHelper.GetResourceKey(icon);
            Assert.Matches(@"^Icon\.[A-Z][a-zA-Z]+\.Regular$", key);
        }
    }

    [Fact]
    public void GetResourceKey_ActionIcons_CorrectFormat()
    {
        var icons = new[] { IconKind.Add, IconKind.Delete, IconKind.Edit, IconKind.Save, IconKind.Copy };
        
        foreach (var icon in icons)
        {
            var key = IconHelper.GetResourceKey(icon);
            Assert.Matches(@"^Icon\.[A-Z][a-zA-Z]+\.Regular$", key);
        }
    }

    #endregion

    #region IconKindToGeometryConverter Tests

    [Fact]
    public void IconKindToGeometryConverter_IsNotNull()
    {
        Assert.NotNull(IconHelper.IconKindToGeometryConverter);
    }

    [Fact]
    public void IconKindToGeometryConverter_IsIValueConverter()
    {
        Assert.IsAssignableFrom<global::Avalonia.Data.Converters.IValueConverter>(IconHelper.IconKindToGeometryConverter);
    }

    #endregion
}

