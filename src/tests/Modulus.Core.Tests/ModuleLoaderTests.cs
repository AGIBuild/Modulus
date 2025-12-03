using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Modulus.Core.Manifest;
using Modulus.Core.Runtime;
using Modulus.Sdk;
using Moq;
using Xunit;

namespace Modulus.Core.Tests;

public class ModuleLoaderTests : IDisposable
{
    private readonly string _testRoot;
    private readonly RuntimeContext _runtimeContext;
    private readonly Mock<IManifestValidator> _mockValidator;
    private readonly Mock<ILogger<ModuleLoader>> _mockLogger;
    private readonly ModuleLoader _loader;

    public ModuleLoaderTests()
    {
        _testRoot = Path.Combine(Path.GetTempPath(), "ModulusTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testRoot);

        _runtimeContext = new RuntimeContext();
        _mockValidator = new Mock<IManifestValidator>();
        _mockLogger = new Mock<ILogger<ModuleLoader>>();
        
        _loader = new ModuleLoader(_runtimeContext, _mockValidator.Object, _mockLogger.Object);
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
        var moduleId = "TestModule";
        var modulePath = CreateTestModule(moduleId, "1.0.0");
        
        _mockValidator.Setup(v => v.ValidateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ModuleManifest>(), It.IsAny<CancellationToken>()))
                      .ReturnsAsync(true);

        // Act
        var result = await _loader.LoadAsync(modulePath);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(moduleId, result.Id);
        
        Assert.True(_runtimeContext.TryGetModule(moduleId, out var runtimeModule));
        Assert.Equal(ModuleState.Loaded, runtimeModule!.State);
    }
    
    [Fact]
    public async Task LoadAsync_InvalidManifest_ReturnsNull()
    {
        // Arrange
        var moduleId = "InvalidModule";
        var modulePath = CreateTestModule(moduleId, "1.0.0"); // Valid structure
        
        // Mock validation failure
        _mockValidator.Setup(v => v.ValidateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ModuleManifest>(), It.IsAny<CancellationToken>()))
                      .ReturnsAsync(false);

        // Act
        var result = await _loader.LoadAsync(modulePath);

        // Assert
        Assert.Null(result);
        Assert.False(_runtimeContext.TryGetModule(moduleId, out _));
    }
    
    [Fact]
    public async Task UnloadAsync_RemovesModule()
    {
        // Arrange
        var moduleId = "UnloadModule";
        var modulePath = CreateTestModule(moduleId, "1.0.0");
        _mockValidator.Setup(v => v.ValidateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ModuleManifest>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        
        await _loader.LoadAsync(modulePath);
        Assert.True(_runtimeContext.TryGetModule(moduleId, out _));

        // Act
        await _loader.UnloadAsync(moduleId);

        // Assert
        Assert.False(_runtimeContext.TryGetModule(moduleId, out _));
    }
    
    [Fact]
    public async Task ReloadAsync_ReloadsModule()
    {
        // Arrange
        var moduleId = "ReloadModule";
        var modulePath = CreateTestModule(moduleId, "1.0.0");
        _mockValidator.Setup(v => v.ValidateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ModuleManifest>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        
        await _loader.LoadAsync(modulePath);
        Assert.True(_runtimeContext.TryGetModule(moduleId, out var module1));

        // Act
        var result = await _loader.ReloadAsync(moduleId);

        // Assert
        Assert.NotNull(result);
        Assert.True(_runtimeContext.TryGetModule(moduleId, out var module2));
        Assert.NotSame(module1, module2); // Should be a new instance
        Assert.Equal(ModuleState.Loaded, module2!.State);
    }

    private string CreateTestModule(string id, string version)
    {
        var modulePath = Path.Combine(_testRoot, id);
        Directory.CreateDirectory(modulePath);

        var manifest = new ModuleManifest
        {
            Id = id,
            Version = version,
            ManifestVersion = "1.0",
            SupportedHosts = new List<string> { "TestHost" },
            CoreAssemblies = new List<string>(), // Empty for basic test
            DisplayName = "Test Module",
            Description = "A test module"
        };

        var manifestJson = JsonSerializer.Serialize(manifest);
        File.WriteAllText(Path.Combine(modulePath, "manifest.json"), manifestJson);
        
        return modulePath;
    }
}

