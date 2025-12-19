using Microsoft.Extensions.Logging;
using Modulus.Core.Runtime;
using Modulus.Infrastructure.Data.Models;
using Modulus.UI.Abstractions;
using NSubstitute;

namespace Modulus.Core.Tests.Runtime;

public class MenuTreeTests
{
    [Fact]
    public void Build_WithParentId_BuildsGroupWithChildren_AndParentHasEmptyNavigationKey()
    {
        var logger = Substitute.For<ILogger>();

        var menus = new List<MenuEntity>
        {
            new()
            {
                Id = "m.avalonia.parent.0",
                ModuleId = "m",
                ParentId = null,
                DisplayName = "Parent",
                Icon = IconKind.Home.ToString(),
                Route = null,
                Location = MenuLocation.Main,
                Order = 1
            },
            new()
            {
                Id = "m.avalonia.child.0",
                ModuleId = "m",
                ParentId = "m.avalonia.parent.0",
                DisplayName = "Child",
                Icon = IconKind.Grid.ToString(),
                Route = "Some.Target",
                Location = MenuLocation.Main,
                Order = 1
            }
        };

        var roots = MenuTreeBuilder.Build(menus, logger);

        Assert.Single(roots);
        var root = roots[0];

        Assert.Equal("m.avalonia.parent.0", root.Id);
        Assert.Equal(string.Empty, root.NavigationKey);
        Assert.NotNull(root.Children);
        Assert.Single(root.Children!);

        var child = root.Children![0];
        Assert.Equal("m.avalonia.child.0", child.Id);
        Assert.Equal("Some.Target", child.NavigationKey);
    }

    [Fact]
    public void Build_WithoutParentId_ReturnsFlatMenu()
    {
        var logger = Substitute.For<ILogger>();

        var menus = new List<MenuEntity>
        {
            new()
            {
                Id = "m.blazor.a.0",
                ModuleId = "m",
                ParentId = null,
                DisplayName = "A",
                Route = "/a",
                Location = MenuLocation.Main,
                Order = 1
            },
            new()
            {
                Id = "m.blazor.b.0",
                ModuleId = "m",
                ParentId = null,
                DisplayName = "B",
                Route = "/b",
                Location = MenuLocation.Main,
                Order = 2
            }
        };

        var roots = MenuTreeBuilder.Build(menus, logger);

        Assert.Equal(2, roots.Count);
        Assert.All(roots, r => Assert.Null(r.Children));
    }
}


