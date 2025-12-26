using Modulus.Core.Architecture;

namespace Modulus.Core.Tests.Architecture;

public sealed class SharedAssemblyOptionsTests
{
    [Fact]
    public void Constants_AreNonEmpty()
    {
        Assert.False(string.IsNullOrWhiteSpace(SharedAssemblyOptions.SectionPath));
        Assert.False(string.IsNullOrWhiteSpace(SharedAssemblyOptions.PrefixesSectionPath));
        Assert.True(SharedAssemblyOptions.MaxAssemblyNameLength > 0);
        Assert.True(SharedAssemblyOptions.MaxManifestHints > 0);
    }

    [Fact]
    public void Defaults_AreInitialized()
    {
        var options = new SharedAssemblyOptions();
        Assert.NotNull(options.Assemblies);
        Assert.NotNull(options.Prefixes);
        Assert.Empty(options.Assemblies);
        Assert.Empty(options.Prefixes);
    }
}


