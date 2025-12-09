using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Modulus.Core.Architecture;
using Modulus.Core.Installation;
using Modulus.Core.Manifest;
using Modulus.Core.Runtime;
using Modulus.Sdk;
using NSubstitute;

namespace Modulus.Core.Tests;

public class ModuleLoaderTests : IDisposable
{
    private readonly string _testRoot;
    private readonly RuntimeContext _runtimeContext;
    private readonly IManifestValidator _validator;
    private readonly ILogger<ModuleLoader> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ModuleLoader _loader;
    private readonly ISharedAssemblyCatalog _sharedCatalog;

    public ModuleLoaderTests()
    {
        _testRoot = Path.Combine(Path.GetTempPath(), "ModulusTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testRoot);

        _runtimeContext = new RuntimeContext();
        _runtimeContext.SetCurrentHost(ModulusHostIds.Avalonia);
        _sharedCatalog = SharedAssemblyCatalog.FromAssemblies(AppDomain.CurrentDomain.GetAssemblies());

        _validator = Substitute.For<IManifestValidator>();
        _logger = Substitute.For<ILogger<ModuleLoader>>();
        _loggerFactory = NullLoggerFactory.Instance;
        
        _loader = new ModuleLoader(_runtimeContext, _validator, _sharedCatalog, _logger, _loggerFactory);
    }

    public void Dispose()
    {
        try 
        { 
            if (Directory.Exists(_testRoot)) 
                Directory.Delete(_testRoot, true); 
        } 
        catch { /* ignore */ }
    }

    [Fact]
    public async Task LoadAsync_ValidModule_ReturnsDescriptorAndRegisters()
    {
        // Arrange
        var moduleId = "test-module-001";
        var modulePath = CreateTestModule(moduleId, "1.0.0");
        
        _validator.ValidateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<VsixManifest>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
                  .Returns(ManifestValidationResult.Success());

        // Act
        var result = await _loader.LoadAsync(modulePath);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(moduleId, result.Id);
        
        Assert.True(_runtimeContext.TryGetModule(moduleId, out var runtimeModule));
        Assert.Equal(ModuleState.Active, runtimeModule!.State);
    }
    
    [Fact]
    public async Task LoadAsync_InvalidManifest_ReturnsNull()
    {
        // Arrange
        var moduleId = "invalid-module-001";
        var modulePath = CreateTestModule(moduleId, "1.0.0");
        
        _validator.ValidateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<VsixManifest>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
                  .Returns(ManifestValidationResult.Failure(new[] { "Test error" }));

        // Act
        var result = await _loader.LoadAsync(modulePath);

        // Assert
        Assert.Null(result);
        Assert.False(_runtimeContext.TryGetModule(moduleId, out _));
    }
    
    [Fact]
    public async Task LoadAsync_NonExistentPath_ReturnsNull()
    {
        // Arrange
        var nonExistentPath = Path.Combine(_testRoot, "non-existent-module");

        // Act
        var result = await _loader.LoadAsync(nonExistentPath);

        // Assert
        Assert.Null(result);
    }
    
    [Fact]
    public async Task LoadAsync_DuplicateModule_ReturnsExistingDescriptor()
    {
        // Arrange
        var moduleId = "duplicate-module-001";
        var modulePath = CreateTestModule(moduleId, "1.0.0");
        
        _validator.ValidateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<VsixManifest>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
                  .Returns(ManifestValidationResult.Success());

        // Act
        var result1 = await _loader.LoadAsync(modulePath);
        var result2 = await _loader.LoadAsync(modulePath);

        // Assert
        Assert.NotNull(result1);
        Assert.NotNull(result2);
        Assert.Equal(result1.Id, result2.Id);
    }
    
    [Fact]
    public async Task UnloadAsync_LoadedModule_RemovesFromContext()
    {
        // Arrange
        var moduleId = "unload-module-001";
        var modulePath = CreateTestModule(moduleId, "1.0.0");
        _validator.ValidateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<VsixManifest>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
                  .Returns(ManifestValidationResult.Success());
        
        await _loader.LoadAsync(modulePath);
        Assert.True(_runtimeContext.TryGetModule(moduleId, out _));

        // Act
        await _loader.UnloadAsync(moduleId);

        // Assert
        Assert.False(_runtimeContext.TryGetModule(moduleId, out _));
    }
    
    [Fact]
    public async Task UnloadAsync_SystemModule_ThrowsException()
    {
        // Arrange
        var moduleId = "system-module-001";
        var modulePath = CreateTestModule(moduleId, "1.0.0");
        _validator.ValidateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<VsixManifest>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
                  .Returns(ManifestValidationResult.Success());
        
        await _loader.LoadAsync(modulePath, isSystem: true);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _loader.UnloadAsync(moduleId));
    }
    
    [Fact]
    public async Task ReloadAsync_LoadedModule_ReloadsSuccessfully()
    {
        // Arrange
        var moduleId = "reload-module-001";
        var modulePath = CreateTestModule(moduleId, "1.0.0");
        _validator.ValidateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<VsixManifest>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
                  .Returns(ManifestValidationResult.Success());
        
        await _loader.LoadAsync(modulePath);
        Assert.True(_runtimeContext.TryGetModule(moduleId, out var originalModule));

        // Act
        var result = await _loader.ReloadAsync(moduleId);

        // Assert
        Assert.NotNull(result);
        Assert.True(_runtimeContext.TryGetModule(moduleId, out var reloadedModule));
        Assert.NotSame(originalModule, reloadedModule);
        Assert.Equal(ModuleState.Active, reloadedModule!.State);
    }
    
    [Fact]
    public async Task ReloadAsync_NonExistentModule_ReturnsNull()
    {
        // Act
        var result = await _loader.ReloadAsync("non-existent-module");

        // Assert
        Assert.Null(result);
    }
    
    [Fact]
    public async Task GetDescriptorAsync_ValidManifest_ReturnsDescriptor()
    {
        // Arrange
        var moduleId = "descriptor-module-001";
        var modulePath = CreateTestModule(moduleId, "2.0.0");

        // Act
        var result = await _loader.GetDescriptorAsync(modulePath);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(moduleId, result.Id);
        Assert.Equal("2.0.0", result.Version);
    }

    [Fact]
    public async Task LoadAsync_MissingDependency_ReturnsNull()
    {
        // Arrange
        var moduleId = "dependent-module-001";
        var modulePath = CreateTestModule(moduleId, "1.0.0", new List<ManifestDependency> { new() { Id = "missing-dep", Version = "[1.0.0]" } });

        _validator.ValidateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<VsixManifest>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(ManifestValidationResult.Success());

        // Act
        var result = await _loader.LoadAsync(modulePath);

        // Assert
        Assert.Null(result);
    }

    private string CreateTestModule(string id, string version, List<ManifestDependency>? dependencies = null)
    {
        var modulePath = Path.Combine(_testRoot, id);
        Directory.CreateDirectory(modulePath);

        XNamespace ns = "http://schemas.microsoft.com/developer/vsx-schema/2011";
        var depElements = (dependencies ?? new List<ManifestDependency>())
            .Select(d => new XElement(ns + "Dependency",
                new XAttribute("Id", d.Id),
                new XAttribute("Version", d.Version)));

        var doc = new XDocument(
            new XElement(ns + "PackageManifest",
                new XAttribute("Version", "2.0.0"),
                new XElement(ns + "Metadata",
                    new XElement(ns + "Identity",
                        new XAttribute("Id", id),
                        new XAttribute("Version", version),
                        new XAttribute("Publisher", "Test")),
                    new XElement(ns + "DisplayName", $"Test Module {id}"),
                    new XElement(ns + "Description", "A test module for unit testing")),
                new XElement(ns + "Installation",
                    new XElement(ns + "InstallationTarget", new XAttribute("Id", ModulusHostIds.Avalonia)),
                    new XElement(ns + "InstallationTarget", new XAttribute("Id", ModulusHostIds.Blazor))),
                new XElement(ns + "Dependencies", depElements),
                new XElement(ns + "Assets",
                    new XElement(ns + "Asset",
                        new XAttribute("Type", ModulusAssetTypes.Package),
                        new XAttribute("Path", "Test.dll")))));

        doc.Save(Path.Combine(modulePath, SystemModuleInstaller.VsixManifestFileName));
        
        return modulePath;
    }
}
