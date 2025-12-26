using System.Reflection;
using System.Reflection.Emit;
using Modulus.Architecture;
using Modulus.Core.Architecture;

namespace Modulus.Core.Tests.Architecture;

public sealed class SharedAssemblyPrefixMismatchTests
{
    [Fact]
    public void IsShared_PrefixMatchOnModuleDomainAssembly_AddsMismatch()
    {
        var asmName = new AssemblyName("Foo.ModuleAsm");
        var asmBuilder = AssemblyBuilder.DefineDynamicAssembly(asmName, AssemblyBuilderAccess.Run);

        // Apply [assembly: AssemblyDomain(AssemblyDomainType.Module)]
        var ctor = typeof(AssemblyDomainAttribute).GetConstructor(new[] { typeof(AssemblyDomainType) });
        Assert.NotNull(ctor);
        var attr = new CustomAttributeBuilder(ctor!, new object[] { AssemblyDomainType.Module });
        asmBuilder.SetCustomAttribute(attr);

        var catalog = SharedAssemblyCatalog.FromAssemblies(
            new[] { asmBuilder },
            configuredAssemblies: null,
            configuredPrefixes: new[] { "Foo." });

        var shared = catalog.IsShared(new AssemblyName("Foo.ModuleAsm"));
        Assert.True(shared);

        var entry = catalog.GetEntries().FirstOrDefault(e => e.Name == "Foo.ModuleAsm");
        Assert.NotNull(entry);
        Assert.True(entry!.HasMismatch);
        Assert.Equal(SharedAssemblyMatchKind.PrefixRule, entry.MatchKind);
        Assert.Equal("Foo.", entry.MatchedPrefix);

        var mismatches = catalog.GetMismatches();
        Assert.Contains(mismatches, m => m.AssemblyName == "Foo.ModuleAsm");
    }
}


