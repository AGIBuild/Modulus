using Modulus.Core.Architecture;
using Modulus.Sdk;

namespace Modulus.Core.Tests.Architecture;

public sealed class SharedAssemblyPolicyTests
{
    [Fact]
    public void GetBuiltInSharedAssemblies_ContainsCoreAssemblies()
    {
        var builtIn = SharedAssemblyPolicy.GetBuiltInSharedAssemblies();
        Assert.Contains("Modulus.Core", builtIn);
        Assert.Contains("Modulus.Sdk", builtIn);
        Assert.Contains("Modulus.UI.Abstractions", builtIn);
    }

    [Fact]
    public void MergeWithConfiguredAssemblies_MergesAndTrims()
    {
        var merged = SharedAssemblyPolicy.MergeWithConfiguredAssemblies(new[] { "  X  ", "", "Y" });
        Assert.Contains("X", merged);
        Assert.Contains("Y", merged);
        Assert.Contains("Modulus.Core", merged); // built-in
    }

    [Fact]
    public void MergeWithConfiguredPrefixes_MergesAndDedupsAndTrims()
    {
        var merged = SharedAssemblyPolicy.MergeWithConfiguredPrefixes(
            configuredPrefixes: new[] { " Avalonia", "Avalonia", "" },
            extraPrefixes: new[] { "Microsoft.Maui.", " Microsoft.Maui. " });

        Assert.Contains("Avalonia", merged);
        Assert.Contains("Microsoft.Maui.", merged);
        Assert.DoesNotContain("", merged);
    }

    [Fact]
    public void GetBuiltInPrefixPresetsForHost_ReturnsExpectedPresets()
    {
        var avalonia = SharedAssemblyPolicy.GetBuiltInPrefixPresetsForHost(ModulusHostIds.Avalonia);
        Assert.Contains("Avalonia", avalonia);

        var blazor = SharedAssemblyPolicy.GetBuiltInPrefixPresetsForHost(ModulusHostIds.Blazor);
        Assert.Contains("Microsoft.Maui.", blazor);
        Assert.Contains("MudBlazor", blazor);

        var unknown = SharedAssemblyPolicy.GetBuiltInPrefixPresetsForHost("Unknown.Host");
        Assert.Empty(unknown);
    }
}


