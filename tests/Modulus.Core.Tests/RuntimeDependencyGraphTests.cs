using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Modulus.Core.Architecture;
using Modulus.Core.Runtime;
using Modulus.Sdk;
using Modulus.UI.Abstractions;
using NSubstitute;

namespace Modulus.Core.Tests;

public class RuntimeDependencyGraphTests
{
    private readonly ISharedAssemblyCatalog _sharedCatalog = SharedAssemblyCatalog.FromAssemblies(AppDomain.CurrentDomain.GetAssemblies());
    private readonly ILogger _logger = Substitute.For<ILogger>();

    [Fact]
    public void TopologicallySort_OrdersByManifestAndDependsOn()
    {
        var moduleA = CreateHandle("ModuleA", "1.0.0", new ModuleA());
        var moduleB = CreateHandle("ModuleB", "1.0.0", new ModuleB()); // DependsOn ModuleA
        var moduleC = CreateHandle("ModuleC", "1.0.0", new ModuleC(), new Dictionary<string, string> { { "ModuleB", "[1.0.0]" } });

        var orderedList = RuntimeDependencyGraph.TopologicallySort(new[] { moduleC, moduleB, moduleA }, _logger).ToList();

        var indexA = orderedList.IndexOf(moduleA);
        var indexB = orderedList.IndexOf(moduleB);
        var indexC = orderedList.IndexOf(moduleC);

        Assert.True(indexA < indexB, "ModuleA should come before ModuleB");
        Assert.True(indexB < indexC, "ModuleB should come before ModuleC due to manifest dependency");
    }

    [Fact]
    public void TopologicallySort_MissingDependency_Throws()
    {
        var moduleA = CreateHandle("ModuleA", "1.0.0", new ModuleA());
        var moduleB = CreateHandle("ModuleB", "1.0.0", new ModuleB(), new Dictionary<string, string> { { "Missing", "[1.0.0]" } });

        Assert.Throws<InvalidOperationException>(() =>
            RuntimeDependencyGraph.TopologicallySort(new[] { moduleA, moduleB }, _logger));
    }

    [Fact]
    public void TopologicallySort_VersionMismatch_Throws()
    {
        var moduleA = CreateHandle("ModuleA", "1.0.0", new ModuleA());
        var moduleB = CreateHandle("ModuleB", "1.0.0", new ModuleB(), new Dictionary<string, string> { { "ModuleA", "[2.0.0]" } });

        Assert.Throws<InvalidOperationException>(() =>
            RuntimeDependencyGraph.TopologicallySort(new[] { moduleA, moduleB }, _logger));
    }

    private RuntimeModuleHandle CreateHandle(string id, string version, IModule moduleInstance, Dictionary<string, string>? manifestDeps = null)
    {
        var descriptor = new ModuleDescriptor(id, version);
        var manifest = new ModuleManifest
        {
            Id = id,
            Version = version,
            DisplayName = id,
            SupportedHosts = new List<string> { HostType.Avalonia },
            CoreAssemblies = new List<string>(),
            UiAssemblies = new Dictionary<string, List<string>>(),
            Dependencies = manifestDeps ?? new Dictionary<string, string>()
        };

        var loadContext = new ModuleLoadContext(id, Path.GetTempPath(), _sharedCatalog);
        var runtimeModule = new RuntimeModule(descriptor, loadContext, Path.GetTempPath(), manifest);
        var provider = new ServiceCollection().BuildServiceProvider();
        var moduleInstances = new List<IModule> { moduleInstance };

        return new RuntimeModuleHandle(runtimeModule, manifest, null, provider, provider, moduleInstances, Array.Empty<MenuItem>(), new[] { moduleInstance.GetType().Assembly });
    }

    private sealed class ModuleA : ModulusComponent { }

    [DependsOn(typeof(ModuleA))]
    private sealed class ModuleB : ModulusComponent { }

    private sealed class ModuleC : ModulusComponent { }
}

