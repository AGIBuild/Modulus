using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.DependencyInjection;
using Modulus.Core;
using Modulus.Core.Installation;
using Modulus.Core.Runtime;
using Modulus.Sdk;
using Modulus.UI.Abstractions;

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
        var moduleDirectories = new List<ModuleDirectory>();
        var dbPath = Path.Combine(_testRoot, "modulus.test.db");

        // Act
        var app = await ModulusApplicationFactory.CreateAsync<TestHostModule>(
            services,
            moduleDirectories,
            ModulusHostIds.Avalonia,
            databasePath: dbPath);

        // Assert
        Assert.NotNull(app);
    }

    [Fact]
    public async Task CreateAsync_WithModuleProvider_LoadsModules()
    {
        // Arrange
        var services = new ServiceCollection();
        var (modulesRoot, moduleDir, moduleId) = CreateMinimalTestModulePackage("integration-test-module", ModulusHostIds.Avalonia);
        var moduleDirectories = new List<ModuleDirectory>
        {
            new ModuleDirectory(modulesRoot, IsSystem: true)
        };
        var dbPath = Path.Combine(_testRoot, "modulus.test.db");

        // Act
        var app = await ModulusApplicationFactory.CreateAsync<TestHostModule>(
            services,
            moduleDirectories,
            ModulusHostIds.Avalonia,
            databasePath: dbPath);
        var serviceProvider = services.BuildServiceProvider();
        app.SetServiceProvider(serviceProvider);

        // Assert
        Assert.NotNull(app);
        var runtimeContext = serviceProvider.GetRequiredService<RuntimeContext>();
        Assert.True(runtimeContext.TryGetModule(moduleId, out _));
    }

    [Fact]
    public async Task InitializeAsync_CallsModuleInitialization()
    {
        // Arrange
        var services = new ServiceCollection();
        var moduleDirectories = new List<ModuleDirectory>();
        var dbPath = Path.Combine(_testRoot, "modulus.test.db");
        
        var app = await ModulusApplicationFactory.CreateAsync<TestHostModule>(
            services,
            moduleDirectories,
            ModulusHostIds.Blazor,
            databasePath: dbPath);
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
        var moduleDirectories = new List<ModuleDirectory>();
        var dbPath = Path.Combine(_testRoot, "modulus.test.db");
        
        var app = await ModulusApplicationFactory.CreateAsync<TestHostModule>(
            services,
            moduleDirectories,
            ModulusHostIds.Avalonia,
            databasePath: dbPath);
        var serviceProvider = services.BuildServiceProvider();
        app.SetServiceProvider(serviceProvider);
        await app.InitializeAsync();

        // Act
        await app.ShutdownAsync();

        // Assert - if no exception, shutdown succeeded
        Assert.True(true);
    }

    [Fact]
    public async Task ModuleLoader_EnableDisableReload_WorksCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        var (modulesRoot, moduleDir, moduleId) = CreateMinimalTestModulePackage("reload-test-module", ModulusHostIds.Avalonia);
        var moduleDirectories = new List<ModuleDirectory>
        {
            // Non-system so it can be unloaded/reloaded in tests
            new ModuleDirectory(modulesRoot, IsSystem: false)
        };
        var dbPath = Path.Combine(_testRoot, "modulus.test.db");

        var app = await ModulusApplicationFactory.CreateAsync<TestHostModule>(
            services,
            moduleDirectories,
            ModulusHostIds.Avalonia,
            databasePath: dbPath);
        var serviceProvider = services.BuildServiceProvider();
        app.SetServiceProvider(serviceProvider);
        await app.InitializeAsync();

        var loader = serviceProvider.GetRequiredService<IModuleLoader>();
        var runtimeContext = serviceProvider.GetRequiredService<RuntimeContext>();

        // Act & Assert - Verify module is loaded
        Assert.True(runtimeContext.TryGetModule(moduleId, out var loadedModule));
        Assert.NotNull(loadedModule);
        Assert.Equal(ModuleState.Active, loadedModule!.State);
        // IMPORTANT: drop references to allow collectible ALC to unload cleanly
        loadedModule = null;

        // Unload
        await loader.UnloadAsync(moduleId);
        Assert.False(runtimeContext.TryGetModule(moduleId, out _));

        // Reload
        var reloaded = await loader.LoadAsync(moduleDir);
        Assert.NotNull(reloaded);
        Assert.True(runtimeContext.TryGetModule(moduleId, out var reloadedModule));
        Assert.Equal(ModuleState.Active, reloadedModule!.State);
    }

    private (string ModulesRoot, string ModuleDir, string ModuleId) CreateMinimalTestModulePackage(string moduleDirName, string targetHost)
    {
        var modulesRoot = Path.Combine(_testRoot, "Modules");
        Directory.CreateDirectory(modulesRoot);

        var moduleDir = Path.Combine(modulesRoot, moduleDirName);
        Directory.CreateDirectory(moduleDir);

        var moduleId = $"{moduleDirName}-{Guid.NewGuid():N}";
        var version = "1.0.0";
        var uiDllName = $"{moduleDirName}.UI.dll";
        var uiDllPath = Path.Combine(moduleDir, uiDllName);

        // Compile a minimal host-specific UI package that:
        // - derives from ModulusPackage (entry point)
        // - declares a host-specific menu attribute
        CompileMinimalUiAssembly(uiDllPath, targetHost);

        // Write extension.vsixmanifest referencing the UI package for the target host.
        WriteManifest(moduleDir, moduleId, version, uiDllName, targetHost);

        Assert.True(File.Exists(Path.Combine(moduleDir, SystemModuleInstaller.VsixManifestFileName)));
        Assert.True(File.Exists(uiDllPath));

        return (modulesRoot, moduleDir, moduleId);
    }

    private static void WriteManifest(string moduleDir, string moduleId, string version, string uiDllName, string targetHost)
    {
        XNamespace ns = "http://schemas.microsoft.com/developer/vsx-schema/2011";
        var doc = new XDocument(
            new XElement(ns + "PackageManifest",
                new XAttribute("Version", "2.0.0"),
                new XElement(ns + "Metadata",
                    new XElement(ns + "Identity",
                        new XAttribute("Id", moduleId),
                        new XAttribute("Version", version),
                        new XAttribute("Language", "en-US"),
                        new XAttribute("Publisher", "Modulus.Tests")),
                    new XElement(ns + "DisplayName", moduleId),
                    new XElement(ns + "Description", "Integration test module")),
                new XElement(ns + "Installation",
                    new XElement(ns + "InstallationTarget",
                        new XAttribute("Id", targetHost),
                        new XAttribute("Version", "[1.0,)"))),
                new XElement(ns + "Assets",
                    // Host-specific UI package (menu attributes must live here)
                    new XElement(ns + "Asset",
                        new XAttribute("Type", ModulusAssetTypes.Package),
                        new XAttribute("Path", uiDllName),
                        new XAttribute("TargetHost", targetHost)))));

        doc.Save(Path.Combine(moduleDir, SystemModuleInstaller.VsixManifestFileName));
    }

    private static void CompileMinimalUiAssembly(string outputPath, string targetHost)
    {
        var isBlazor = ModulusHostIds.Matches(targetHost, ModulusHostIds.Blazor);
        var isAvalonia = ModulusHostIds.Matches(targetHost, ModulusHostIds.Avalonia);

        if (!isBlazor && !isAvalonia)
            throw new InvalidOperationException($"Unsupported test host '{targetHost}'.");

        var code = isBlazor
            ? """
              using Modulus.Sdk;
              using Modulus.UI.Abstractions;

              namespace Modulus.IntegrationTests;

              [BlazorMenu("test", "Test", "/test", Icon = IconKind.Grid, Order = 10, Location = MenuLocation.Main)]
              public sealed class TestModule : ModulusPackage
              {
                  public override void ConfigureServices(IModuleLifecycleContext context) { }
              }
              """
            : """
              using Modulus.Sdk;
              using Modulus.UI.Abstractions;

              namespace Modulus.IntegrationTests;

              public sealed class TestViewModel { }

              [AvaloniaMenu("test", "Test", typeof(TestViewModel), Icon = IconKind.Grid, Order = 10, Location = MenuLocation.Main)]
              public sealed class TestModule : ModulusPackage
              {
                  public override void ConfigureServices(IModuleLifecycleContext context) { }
              }
              """;

        var syntaxTree = CSharpSyntaxTree.ParseText(code);

        var references = new List<MetadataReference>
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(ModulusPackage).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(MenuLocation).Assembly.Location),
        };

        // Add common runtime refs (avoid missing System.Runtime on some runners)
        var runtimeDir = Path.GetDirectoryName(typeof(object).Assembly.Location);
        if (!string.IsNullOrWhiteSpace(runtimeDir))
        {
            foreach (var name in new[] { "System.Runtime.dll", "netstandard.dll" })
            {
                var p = Path.Combine(runtimeDir, name);
                if (File.Exists(p))
                    references.Add(MetadataReference.CreateFromFile(p));
            }
        }

        var compilation = CSharpCompilation.Create(
            Path.GetFileNameWithoutExtension(outputPath),
            new[] { syntaxTree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, optimizationLevel: OptimizationLevel.Release));

        var emit = compilation.Emit(outputPath);
        if (!emit.Success)
        {
            var diag = string.Join(Environment.NewLine, emit.Diagnostics.Select(d => d.ToString()));
            throw new InvalidOperationException($"Failed to compile test module UI assembly:{Environment.NewLine}{diag}");
        }
    }

    // Test host module
    private class TestHostModule : ModulusPackage
    {
        public override void ConfigureServices(IModuleLifecycleContext context)
        {
            // Minimal configuration for testing
        }
    }
}
