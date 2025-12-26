using System.Reflection;
using Modulus.Core.Architecture;

namespace Modulus.Core.Tests.Architecture;

public sealed class SharedAssemblyPrefixRulesTests
{
    [Fact]
    public void FromAssemblies_StoresConfiguredPrefixes()
    {
        var prefixes = new[] { " Avalonia", "", "  ", "Microsoft.Maui." };
        var catalog = SharedAssemblyCatalog.FromAssemblies(Array.Empty<Assembly>(), configuredAssemblies: null, configuredPrefixes: prefixes);

        var stored = catalog.GetPrefixRules();
        Assert.Contains("Avalonia", stored);
        Assert.Contains("Microsoft.Maui.", stored);
    }

    [Fact]
    public void IsShared_ReturnsTrueForPrefixMatch_AndCreatesEntry()
    {
        var prefixes = new[] { "Foo." };
        var catalog = SharedAssemblyCatalog.FromAssemblies(Array.Empty<Assembly>(), configuredAssemblies: null, configuredPrefixes: prefixes);

        var isShared = catalog.IsShared(new AssemblyName("Foo.Bar"));

        Assert.True(isShared);

        var entry = catalog.GetEntries().FirstOrDefault(e => e.Name == "Foo.Bar");
        Assert.NotNull(entry);
        Assert.Equal(SharedAssemblyMatchKind.PrefixRule, entry!.MatchKind);
        Assert.Equal("Foo.", entry.MatchedPrefix);
    }
}


