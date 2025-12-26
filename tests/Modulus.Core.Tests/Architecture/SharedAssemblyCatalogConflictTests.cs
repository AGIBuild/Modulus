using System.Reflection;
using System.Reflection.Emit;
using Modulus.Architecture;
using Modulus.Core.Architecture;

namespace Modulus.Core.Tests.Architecture;

public sealed class SharedAssemblyCatalogConflictTests
{
    private static Assembly CreateDynamicAssemblyWithDomain(string simpleName, AssemblyDomainType domainType)
    {
        var asmName = new AssemblyName(simpleName);
        var asmBuilder = AssemblyBuilder.DefineDynamicAssembly(asmName, AssemblyBuilderAccess.Run);

        var ctor = typeof(AssemblyDomainAttribute).GetConstructor(new[] { typeof(AssemblyDomainType) });
        var attr = new CustomAttributeBuilder(ctor!, new object[] { domainType });
        asmBuilder.SetCustomAttribute(attr);

        return asmBuilder;
    }

    [Fact]
    public void FromAssemblies_WhenHostConfigAddsModuleDomainAssembly_AddsMismatchEntry()
    {
        var moduleAsm = CreateDynamicAssemblyWithDomain("Conflicting.Assembly", AssemblyDomainType.Module);

        var catalog = SharedAssemblyCatalog.FromAssemblies(
            new[] { moduleAsm },
            configuredAssemblies: new[] { "Conflicting.Assembly" });

        Assert.Contains("Conflicting.Assembly", catalog.Names);

        var entry = catalog.GetEntries().First(e => e.Name == "Conflicting.Assembly");
        Assert.True(entry.HasMismatch);
        Assert.Equal(SharedAssemblySource.HostConfig, entry.Source);

        Assert.Contains(catalog.GetMismatches(), m => m.AssemblyName == "Conflicting.Assembly");
    }

    [Fact]
    public void IsShared_WhenEntryExistsButDeclaredModuleDomain_StillReturnsTrue()
    {
        var moduleAsm = CreateDynamicAssemblyWithDomain("SharedButModule", AssemblyDomainType.Module);

        var catalog = SharedAssemblyCatalog.FromAssemblies(
            new[] { moduleAsm },
            configuredAssemblies: new[] { "SharedButModule" });

        Assert.True(catalog.IsShared(new AssemblyName("SharedButModule")));
    }

    [Fact]
    public void AddManifestHints_WhenHintIsModuleDomain_AddsMismatchAndStillAddsEntry()
    {
        var moduleAsm = CreateDynamicAssemblyWithDomain("Hinted.ModuleAsm", AssemblyDomainType.Module);
        var catalog = SharedAssemblyCatalog.FromAssemblies(new[] { moduleAsm });

        var mismatches = catalog.AddManifestHints("module1", new[] { "Hinted.ModuleAsm" });

        Assert.Single(mismatches);
        Assert.Contains(mismatches, m => m.AssemblyName == "Hinted.ModuleAsm");
        Assert.Contains("Hinted.ModuleAsm", catalog.Names);

        var entry = catalog.GetEntries().First(e => e.Name == "Hinted.ModuleAsm");
        Assert.True(entry.HasMismatch);
        Assert.Equal(SharedAssemblySource.ManifestHint, entry.Source);
    }
}


