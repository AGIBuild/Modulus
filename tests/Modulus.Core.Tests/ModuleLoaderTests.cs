using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Modulus.Core.Architecture;
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
        _runtimeContext.SetCurrentHost(HostType.Avalonia);
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
        
        _validator.ValidateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<ModuleManifest>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
                  .Returns(true);

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
        
        _validator.ValidateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<ModuleManifest>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
                  .Returns(false);

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
        
        _validator.ValidateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<ModuleManifest>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
                  .Returns(true);

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
        _validator.ValidateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<ModuleManifest>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
                  .Returns(true);
        
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
        _validator.ValidateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<ModuleManifest>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
                  .Returns(true);
        
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
        _validator.ValidateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<ModuleManifest>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
                  .Returns(true);
        
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
        var modulePath = CreateTestModule(moduleId, "1.0.0", new Dictionary<string, string> { { "missing-dep", "[1.0.0]" } });

        _validator.ValidateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<ModuleManifest>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(true);

        // Act
        var result = await _loader.LoadAsync(modulePath);

        // Assert
        Assert.Null(result);
    }

    private string CreateTestModule(string id, string version, Dictionary<string, string>? dependencies = null)
    {
        var modulePath = Path.Combine(_testRoot, id);
        Directory.CreateDirectory(modulePath);

        var manifest = new ModuleManifest
        {
            Id = id,
            Version = version,
            ManifestVersion = "1.0",
            SupportedHosts = new List<string> { HostType.Avalonia, HostType.Blazor },
            CoreAssemblies = new List<string>(),
            DisplayName = $"Test Module {id}",
            Description = "A test module for unit testing",
            Dependencies = dependencies ?? new Dictionary<string, string>()
        };

        var manifestJson = JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(Path.Combine(modulePath, "manifest.json"), manifestJson);
        
        return modulePath;
    }
}

