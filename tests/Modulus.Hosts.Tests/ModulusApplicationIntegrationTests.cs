using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Modulus.Core;
using Modulus.Core.Runtime;
using Modulus.Sdk;

namespace Modulus.Hosts.Tests;

/// <summary>
/// Integration tests for ModulusApplication lifecycle.
/// </summary>
public class ModulusApplicationIntegrationTests : IDisposable
{
    private readonly string _testRoot;

    public ModulusApplicationIntegrationTests()
    {
        _testRoot = Path.Combine(Path.GetTempPath(), "ModulusIntegrationTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testRoot);
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
    public async Task CreateAsync_WithNoModules_CreatesApplication()
    {
        // Arrange
        var services = new ServiceCollection();
        var providers = new List<IModuleProvider>();

        // Act
        var app = await ModulusApplicationFactory.CreateAsync<TestHostModule>(services, providers, HostType.Avalonia);

        // Assert
        Assert.NotNull(app);
    }

    [Fact(Skip = "Requires real module package with valid coreAssemblies and uiAssemblies. Use actual module builds for integration testing.")]
    public async Task CreateAsync_WithModuleProvider_LoadsModules()
    {
        // Arrange
        var services = new ServiceCollection();
        var modulePath = CreateTestModule("integration-test-module", "1.0.0");
        var providers = new List<IModuleProvider>
        {
            new DirectoryModuleProvider(_testRoot, Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance, isSystem: true)
        };

        // Act
        var app = await ModulusApplicationFactory.CreateAsync<TestHostModule>(services, providers, HostType.Avalonia);
        var serviceProvider = services.BuildServiceProvider();
        app.SetServiceProvider(serviceProvider);

        // Assert
        Assert.NotNull(app);
        var runtimeContext = serviceProvider.GetRequiredService<RuntimeContext>();
        Assert.True(runtimeContext.TryGetModule("integration-test-module", out _));
    }

    [Fact]
    public async Task InitializeAsync_CallsModuleInitialization()
    {
        // Arrange
        var services = new ServiceCollection();
        var providers = new List<IModuleProvider>();
        
        var app = await ModulusApplicationFactory.CreateAsync<TestHostModule>(services, providers, HostType.Blazor);
        var serviceProvider = services.BuildServiceProvider();
        app.SetServiceProvider(serviceProvider);

        // Act
        await app.InitializeAsync();

        // Assert - if no exception, initialization succeeded
        Assert.NotNull(app.ServiceProvider);
    }

    [Fact]
    public async Task ShutdownAsync_CompletesSuccessfully()
    {
        // Arrange
        var services = new ServiceCollection();
        var providers = new List<IModuleProvider>();
        
        var app = await ModulusApplicationFactory.CreateAsync<TestHostModule>(services, providers, HostType.Avalonia);
        var serviceProvider = services.BuildServiceProvider();
        app.SetServiceProvider(serviceProvider);
        await app.InitializeAsync();

        // Act
        await app.ShutdownAsync();

        // Assert - if no exception, shutdown succeeded
        Assert.True(true);
    }

    [Fact(Skip = "Requires real module package with valid coreAssemblies and uiAssemblies. Use actual module builds for integration testing.")]
    public async Task ModuleLoader_EnableDisableReload_WorksCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        var modulePath = CreateTestModule("reload-test-module", "1.0.0");
        var providers = new List<IModuleProvider>
        {
            new DirectoryModuleProvider(_testRoot, Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance, isSystem: true)
        };

        var app = await ModulusApplicationFactory.CreateAsync<TestHostModule>(services, providers, HostType.Avalonia);
        var serviceProvider = services.BuildServiceProvider();
        app.SetServiceProvider(serviceProvider);
        await app.InitializeAsync();

        var loader = serviceProvider.GetRequiredService<IModuleLoader>();
        var runtimeContext = serviceProvider.GetRequiredService<RuntimeContext>();

        // Act & Assert - Verify module is loaded
        Assert.True(runtimeContext.TryGetModule("reload-test-module", out var module));
        Assert.Equal(ModuleState.Active, module!.State);

        // Unload
        await loader.UnloadAsync("reload-test-module");
        Assert.False(runtimeContext.TryGetModule("reload-test-module", out _));

        // Reload
        var reloaded = await loader.LoadAsync(modulePath);
        Assert.NotNull(reloaded);
        Assert.True(runtimeContext.TryGetModule("reload-test-module", out var reloadedModule));
        Assert.Equal(ModuleState.Active, reloadedModule!.State);
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
            SupportedHosts = new List<string> { HostType.Avalonia, HostType.Blazor },
            CoreAssemblies = new List<string>(),
            DisplayName = $"Test Module {id}",
            Description = "A test module for integration testing"
        };

        var manifestJson = JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(Path.Combine(modulePath, "manifest.json"), manifestJson);

        return modulePath;
    }

    // Test host module
    private class TestHostModule : ModulusComponent
    {
        public override void ConfigureServices(IModuleLifecycleContext context)
        {
            // Minimal configuration for testing
        }
    }
}

