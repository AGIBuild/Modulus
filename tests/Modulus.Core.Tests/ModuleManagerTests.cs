using Microsoft.Extensions.Logging;
using Modulus.Core.Runtime;
using Modulus.Sdk;
using NSubstitute;

namespace Modulus.Core.Tests;

public class ModuleManagerTests
{
    private readonly ILogger<ModuleManager> _logger;
    private readonly ModuleManager _manager;

    public ModuleManagerTests()
    {
        _logger = Substitute.For<ILogger<ModuleManager>>();
        _manager = new ModuleManager(_logger);
    }

    [Fact]
    public void AddModule_AddsModuleToManager()
    {
        // Arrange
        var module = new TestModule();

        // Act
        _manager.AddModule(module);

        // Assert
        var sorted = _manager.GetSortedModules();
        Assert.Contains(module, sorted);
    }

    [Fact]
    public void GetSortedModules_ReturnsModulesInDependencyOrder()
    {
        // Arrange
        var moduleA = new TestModuleA();
        var moduleB = new TestModuleB(); // Depends on A
        
        // Add in reverse order
        _manager.AddModule(moduleB);
        _manager.AddModule(moduleA);

        // Act
        var sorted = _manager.GetSortedModules().ToList();

        // Assert
        var indexA = sorted.IndexOf(moduleA);
        var indexB = sorted.IndexOf(moduleB);
        Assert.True(indexA < indexB, "Module A should come before Module B (dependency)");
    }

    [Fact]
    public void GetSortedModules_HandlesNoDependencies()
    {
        // Arrange
        var module1 = new TestModule();
        var module2 = new TestModule2();
        
        _manager.AddModule(module1);
        _manager.AddModule(module2);

        // Act
        var sorted = _manager.GetSortedModules();

        // Assert
        Assert.Equal(2, sorted.Count);
    }

    [Fact]
    public void GetSortedModules_MissingDependency_Throws()
    {
        // Arrange
        var moduleWithMissingDep = new ModuleWithMissingDependency();
        _manager.AddModule(moduleWithMissingDep);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => _manager.GetSortedModules());
    }

    // Test module classes
    private class TestModule : ModulusPackage { }
    private class TestModule2 : ModulusPackage { }
    
    private class TestModuleA : ModulusPackage { }
    
    [DependsOn(typeof(TestModuleA))]
    private class TestModuleB : ModulusPackage { }

    [DependsOn(typeof(ExternalDependency))]
    private class ModuleWithMissingDependency : ModulusPackage { }

    private class ExternalDependency : ModulusPackage { }
}

