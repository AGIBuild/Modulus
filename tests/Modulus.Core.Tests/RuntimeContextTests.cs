using Modulus.Core.Runtime;
using Modulus.Core.Architecture;

namespace Modulus.Core.Tests;

public class RuntimeContextTests
{
    [Fact]
    public void SetCurrentHost_SetsHostType()
    {
        // Arrange
        var context = new RuntimeContext();

        // Act
        context.SetCurrentHost(HostType.Blazor);

        // Assert
        Assert.Equal(HostType.Blazor, context.HostType);
    }

    [Fact]
    public void RegisterModule_AddsModuleToContext()
    {
        // Arrange
        var context = new RuntimeContext();
        var descriptor = new ModuleDescriptor("test-module", "1.0.0", "Test", "Description", new[] { HostType.Avalonia });
        var sharedCatalog = SharedAssemblyCatalog.FromAssemblies(AppDomain.CurrentDomain.GetAssemblies());
        var loadContext = new ModuleLoadContext("test-module", "/path/to/module", sharedCatalog);
        var manifest = new Modulus.Sdk.ModuleManifest { Id = "test-module", Version = "1.0.0" };
        var runtimeModule = new RuntimeModule(descriptor, loadContext, "/path/to/module", manifest, false);

        // Act
        context.RegisterModule(runtimeModule);

        // Assert
        Assert.True(context.TryGetModule("test-module", out var retrieved));
        Assert.Same(runtimeModule, retrieved);
    }

    [Fact]
    public void RemoveModule_RemovesModuleFromContext()
    {
        // Arrange
        var context = new RuntimeContext();
        var descriptor = new ModuleDescriptor("test-module", "1.0.0", "Test", "Description", new[] { HostType.Avalonia });
        var sharedCatalog = SharedAssemblyCatalog.FromAssemblies(AppDomain.CurrentDomain.GetAssemblies());
        var loadContext = new ModuleLoadContext("test-module", "/path/to/module", sharedCatalog);
        var manifest = new Modulus.Sdk.ModuleManifest { Id = "test-module", Version = "1.0.0" };
        var runtimeModule = new RuntimeModule(descriptor, loadContext, "/path/to/module", manifest, false);
        context.RegisterModule(runtimeModule);

        // Act
        context.RemoveModule("test-module");

        // Assert
        Assert.False(context.TryGetModule("test-module", out _));
    }

    [Fact]
    public void TryGetModule_NonExistent_ReturnsFalse()
    {
        // Arrange
        var context = new RuntimeContext();

        // Act
        var result = context.TryGetModule("non-existent", out var module);

        // Assert
        Assert.False(result);
        Assert.Null(module);
    }

    [Fact]
    public void RuntimeModules_ReturnsAllRegisteredModules()
    {
        // Arrange
        var context = new RuntimeContext();
        
        var descriptor1 = new ModuleDescriptor("module-1", "1.0.0", "Module 1", "Desc", new[] { HostType.Avalonia });
        var sharedCatalog = SharedAssemblyCatalog.FromAssemblies(AppDomain.CurrentDomain.GetAssemblies());
        var loadContext1 = new ModuleLoadContext("module-1", "/path/1", sharedCatalog);
        var manifest1 = new Modulus.Sdk.ModuleManifest { Id = "module-1", Version = "1.0.0" };
        var module1 = new RuntimeModule(descriptor1, loadContext1, "/path/1", manifest1, false);
        
        var descriptor2 = new ModuleDescriptor("module-2", "1.0.0", "Module 2", "Desc", new[] { HostType.Blazor });
        var loadContext2 = new ModuleLoadContext("module-2", "/path/2", sharedCatalog);
        var manifest2 = new Modulus.Sdk.ModuleManifest { Id = "module-2", Version = "1.0.0" };
        var module2 = new RuntimeModule(descriptor2, loadContext2, "/path/2", manifest2, false);
        
        context.RegisterModule(module1);
        context.RegisterModule(module2);

        // Act
        var modules = context.RuntimeModules;

        // Assert
        Assert.Equal(2, modules.Count);
        Assert.Contains(modules, m => m.Descriptor.Id == "module-1");
        Assert.Contains(modules, m => m.Descriptor.Id == "module-2");
    }
}

