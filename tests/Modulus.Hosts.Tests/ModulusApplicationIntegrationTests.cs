using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Modulus.Core;
using Modulus.Core.Installation;
using Modulus.Core.Runtime;
using Modulus.Host.Avalonia.Services;
using Modulus.Host.Avalonia.Shell.Services;
using Modulus.Infrastructure.Data;
using Modulus.Infrastructure.Data.Repositories;
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

    [Fact]
    public async Task EndToEnd_SingleView_ViewMenuDeclared_IsCollapsed_DbAndRuntimeAndNavigationWork()
    {
        // Arrange
        var services = new ServiceCollection();
        var (modulesRoot, _, moduleId) = CreateMenuScenarioTestModulePackage("e2e-single", ModulusHostIds.Avalonia, MenuScenario.SingleViewWithViewMenuDeclared);
        var moduleDirectories = new List<ModuleDirectory>
        {
            new ModuleDirectory(modulesRoot, IsSystem: true)
        };
        var dbPath = Path.Combine(_testRoot, "modulus.e2e.single.db");

        // Act: Create app (installs modules -> projects menus into DB)
        var app = await ModulusApplicationFactory.CreateAsync<TestHostModule>(
            services,
            moduleDirectories,
            ModulusHostIds.Avalonia,
            databasePath: dbPath);

        // Assert DB projection: single-view collapse => only parent menu exists, no ParentId rows for this module
        using (var db = OpenDb(dbPath))
        {
            var moduleMenus = await db.Menus.AsNoTracking().Where(m => m.ModuleId == moduleId).ToListAsync();
            Assert.Single(moduleMenus);
            Assert.All(moduleMenus, m => Assert.True(string.IsNullOrWhiteSpace(m.ParentId)));
        }

        // Act: bind runtime services and initialize (register menus from DB into IMenuRegistry)
        RegisterRuntimeDbAndMenuServices(services, dbPath);
        var serviceProvider = services.BuildServiceProvider();
        app.SetServiceProvider(serviceProvider);
        await app.InitializeAsync();

        // Assert runtime menu tree: single leaf, no children
        var menuRegistry = serviceProvider.GetRequiredService<IMenuRegistry>();
        var moduleRoot = menuRegistry.GetItems(MenuLocation.Main).Single(i => i.ModuleId == moduleId);
        Assert.Null(moduleRoot.Children);
        Assert.False(string.IsNullOrWhiteSpace(moduleRoot.NavigationKey));

        // Assert navigation resolves by stable menu id
        var nav = new AvaloniaNavigationService(
            serviceProvider,
            serviceProvider.GetRequiredService<IUIFactory>(),
            menuRegistry,
            serviceProvider.GetRequiredService<RuntimeContext>(),
            serviceProvider.GetRequiredService<ILazyModuleLoader>(),
            serviceProvider.GetRequiredService<IModuleExecutionGuard>(),
            serviceProvider.GetRequiredService<ILogger<AvaloniaNavigationService>>());

        object? navigatedVm = null;
        nav.Navigated += (_, e) => navigatedVm = e.ViewModel;

        var ok = await nav.NavigateToAsync(moduleRoot.Id);
        Assert.True(ok);
        Assert.Equal(moduleRoot.Id, nav.CurrentNavigationKey);
        Assert.NotNull(navigatedVm);
        Assert.Equal("Modulus.IntegrationTests.SingleViewModel", navigatedVm!.GetType().FullName);
    }

    [Fact]
    public async Task EndToEnd_MultiView_ProjectsHierarchy_RuntimeBuildsTree_ParentNotNavigable_ChildrenNavigable()
    {
        // Arrange
        var services = new ServiceCollection();
        var (modulesRoot, _, moduleId) = CreateMenuScenarioTestModulePackage("e2e-multi", ModulusHostIds.Avalonia, MenuScenario.MultiViewWithViewMenusDeclared);
        var moduleDirectories = new List<ModuleDirectory>
        {
            new ModuleDirectory(modulesRoot, IsSystem: true)
        };
        var dbPath = Path.Combine(_testRoot, "modulus.e2e.multi.db");

        // Act: Create app (installs modules -> projects menus into DB)
        var app = await ModulusApplicationFactory.CreateAsync<TestHostModule>(
            services,
            moduleDirectories,
            ModulusHostIds.Avalonia,
            databasePath: dbPath);

        // Assert DB projection: parent + 2 children with ParentId set
        using (var db = OpenDb(dbPath))
        {
            var moduleMenus = await db.Menus.AsNoTracking().Where(m => m.ModuleId == moduleId).OrderBy(m => m.Order).ToListAsync();
            Assert.Equal(3, moduleMenus.Count);

            var roots = moduleMenus.Where(m => string.IsNullOrWhiteSpace(m.ParentId)).ToList();
            Assert.Single(roots);

            var parent = roots.Single();
            var children = moduleMenus.Where(m => string.Equals(m.ParentId, parent.Id, StringComparison.OrdinalIgnoreCase)).ToList();
            Assert.Equal(2, children.Count);
        }

        // Act: bind runtime services and initialize (register menus from DB into IMenuRegistry)
        RegisterRuntimeDbAndMenuServices(services, dbPath);
        var serviceProvider = services.BuildServiceProvider();
        app.SetServiceProvider(serviceProvider);
        await app.InitializeAsync();

        // Assert runtime tree: parent is group (NavigationKey empty), children are leaf items
        var menuRegistry = serviceProvider.GetRequiredService<IMenuRegistry>();
        var moduleRoot = menuRegistry.GetItems(MenuLocation.Main).Single(i => i.ModuleId == moduleId);
        Assert.NotNull(moduleRoot.Children);
        Assert.Equal(string.Empty, moduleRoot.NavigationKey);
        Assert.Equal(2, moduleRoot.Children!.Count);
        Assert.All(moduleRoot.Children, c => Assert.False(string.IsNullOrWhiteSpace(c.NavigationKey)));

        var childA = moduleRoot.Children.First(c => c.DisplayName == "View A");
        var childB = moduleRoot.Children.First(c => c.DisplayName == "View B");

        var nav = new AvaloniaNavigationService(
            serviceProvider,
            serviceProvider.GetRequiredService<IUIFactory>(),
            menuRegistry,
            serviceProvider.GetRequiredService<RuntimeContext>(),
            serviceProvider.GetRequiredService<ILazyModuleLoader>(),
            serviceProvider.GetRequiredService<IModuleExecutionGuard>(),
            serviceProvider.GetRequiredService<ILogger<AvaloniaNavigationService>>());

        // Parent is a group: not navigable
        var parentOk = await nav.NavigateToAsync(moduleRoot.Id);
        Assert.False(parentOk);

        // Child A navigable
        object? vmA = null;
        nav.Navigated += (_, e) => vmA = e.ViewModel;
        var okA = await nav.NavigateToAsync(childA.Id);
        Assert.True(okA);
        Assert.Equal(childA.Id, nav.CurrentNavigationKey);
        Assert.NotNull(vmA);
        Assert.Equal("Modulus.IntegrationTests.ViewModelA", vmA!.GetType().FullName);

        // Child B navigable
        object? vmB = null;
        nav.Navigated += (_, e) => vmB = e.ViewModel;
        var okB = await nav.NavigateToAsync(childB.Id, new NavigationOptions { ForceNewInstance = true });
        Assert.True(okB);
        Assert.Equal(childB.Id, nav.CurrentNavigationKey);
        Assert.NotNull(vmB);
        Assert.Equal("Modulus.IntegrationTests.ViewModelB", vmB!.GetType().FullName);
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

              public sealed class TestViewModel : ViewModelBase
              {
                  public TestViewModel()
                  {
                      Title = "Test";
                  }
              }

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
            MetadataReference.CreateFromFile(typeof(CommunityToolkit.Mvvm.ComponentModel.ObservableObject).Assembly.Location),
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

    private enum MenuScenario
    {
        SingleViewWithViewMenuDeclared,
        MultiViewWithViewMenusDeclared
    }

    private (string ModulesRoot, string ModuleDir, string ModuleId) CreateMenuScenarioTestModulePackage(
        string moduleDirName,
        string targetHost,
        MenuScenario scenario)
    {
        var modulesRoot = Path.Combine(_testRoot, "Modules");
        Directory.CreateDirectory(modulesRoot);

        var moduleDir = Path.Combine(modulesRoot, moduleDirName);
        Directory.CreateDirectory(moduleDir);

        var moduleId = $"{moduleDirName}-{Guid.NewGuid():N}";
        var version = "1.0.0";
        var uiDllName = $"{moduleDirName}.UI.dll";
        var uiDllPath = Path.Combine(moduleDir, uiDllName);

        CompileMenuScenarioUiAssembly(uiDllPath, targetHost, scenario);
        WriteManifest(moduleDir, moduleId, version, uiDllName, targetHost);

        Assert.True(File.Exists(Path.Combine(moduleDir, SystemModuleInstaller.VsixManifestFileName)));
        Assert.True(File.Exists(uiDllPath));

        return (modulesRoot, moduleDir, moduleId);
    }

    private static void CompileMenuScenarioUiAssembly(string outputPath, string targetHost, MenuScenario scenario)
    {
        if (!ModulusHostIds.Matches(targetHost, ModulusHostIds.Avalonia))
            throw new InvalidOperationException($"This menu scenario test currently supports only Avalonia. Host='{targetHost}'.");

        var code = scenario switch
        {
            MenuScenario.SingleViewWithViewMenuDeclared => """
              using Modulus.Sdk;
              using Modulus.UI.Abstractions;

              namespace Modulus.IntegrationTests;

              [AvaloniaViewMenu("view", "View", Icon = IconKind.Grid, Order = 20, Location = MenuLocation.Main)]
              public sealed class SingleViewModel : ViewModelBase
              {
                  public SingleViewModel()
                  {
                      Title = "View";
                  }
              }

              [AvaloniaMenu("module", "Module", typeof(SingleViewModel), Icon = IconKind.Folder, Order = 10, Location = MenuLocation.Main)]
              public sealed class TestModule : ModulusPackage
              {
                  public override void ConfigureServices(IModuleLifecycleContext context) { }
              }
              """,

            MenuScenario.MultiViewWithViewMenusDeclared => """
              using Modulus.Sdk;
              using Modulus.UI.Abstractions;

              namespace Modulus.IntegrationTests;

              [AvaloniaViewMenu("a", "View A", Icon = IconKind.Grid, Order = 20, Location = MenuLocation.Main)]
              public sealed class ViewModelA : ViewModelBase
              {
                  public ViewModelA()
                  {
                      Title = "View A";
                  }
              }

              [AvaloniaViewMenu("b", "View B", Icon = IconKind.Grid, Order = 30, Location = MenuLocation.Main)]
              public sealed class ViewModelB : ViewModelBase
              {
                  public ViewModelB()
                  {
                      Title = "View B";
                  }
              }

              [AvaloniaMenu("module", "Module", typeof(ViewModelA), Icon = IconKind.Folder, Order = 10, Location = MenuLocation.Main)]
              public sealed class TestModule : ModulusPackage
              {
                  public override void ConfigureServices(IModuleLifecycleContext context) { }
              }
              """,

            _ => throw new ArgumentOutOfRangeException(nameof(scenario), scenario, "Unknown menu scenario")
        };

        var syntaxTree = CSharpSyntaxTree.ParseText(code);

        var references = new List<MetadataReference>
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(ModulusPackage).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(MenuLocation).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(CommunityToolkit.Mvvm.ComponentModel.ObservableObject).Assembly.Location),
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

    private static ModulusDbContext OpenDb(string databasePath)
    {
        var options = new DbContextOptionsBuilder<ModulusDbContext>()
            .UseSqlite($"Data Source={databasePath}")
            .Options;

        return new ModulusDbContext(options);
    }

    private static void RegisterRuntimeDbAndMenuServices(IServiceCollection services, string databasePath)
    {
        services.AddDbContext<ModulusDbContext>(options => options.UseSqlite($"Data Source={databasePath}"));
        services.AddScoped<IModuleRepository, ModuleRepository>();
        services.AddScoped<IMenuRepository, MenuRepository>();

        // Runtime menu registry used by ModulusApplication.InitializeAsync() to register hierarchical menu trees
        services.AddSingleton<IMenuRegistry, MenuRegistry>();

        // Avalonia navigation dependencies (we keep UI creation trivial in tests)
        services.AddSingleton<IUIFactory, TestUIFactory>();
        services.AddSingleton<ILazyModuleLoader, LazyModuleLoader>();
    }

    private sealed class TestUIFactory : IUIFactory
    {
        public object CreateView(object viewModel) => new object();
        public object CreateView(string viewKey) => new object();
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
