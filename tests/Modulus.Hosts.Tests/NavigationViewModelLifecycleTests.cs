using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Modulus.Core.Architecture;
using Modulus.Core.Runtime;
using Modulus.Host.Avalonia.Services;
using Modulus.Sdk;
using Modulus.UI.Abstractions;
using NSubstitute;

namespace Modulus.Hosts.Tests;

public class NavigationViewModelLifecycleTests
{
    [Fact]
    public async Task Navigate_Calls_Guards_Then_CurrentVM_Then_TargetVM_Then_LifecycleCallbacks()
    {
        var calls = new List<string>();

        var moduleId = "TestModule";

        var services = new ServiceCollection();
        services.AddSingleton(new CallRecorder(calls));
        services.AddTransient<TestVmA>();
        services.AddTransient<TestVmB>();
        var hostProvider = services.BuildServiceProvider();
        var runtimeContext = CreateRuntimeContextWithModuleHandle(moduleId, typeof(TestVmA).Assembly, hostProvider);

        var menuA = new MenuItem($"{moduleId}.avalonia.a.0", "A", IconKind.Home, typeof(TestVmA).FullName!);
        menuA.ModuleId = moduleId;
        var menuB = new MenuItem($"{moduleId}.avalonia.b.0", "B", IconKind.Grid, typeof(TestVmB).FullName!);
        menuB.ModuleId = moduleId;

        var menuRegistry = Substitute.For<IMenuRegistry>();
        menuRegistry.GetItems(MenuLocation.Main).Returns(new[] { menuA, menuB });
        menuRegistry.GetItems(MenuLocation.Bottom).Returns(Array.Empty<MenuItem>());

        var lazyLoader = Substitute.For<ILazyModuleLoader>();
        lazyLoader.EnsureModuleLoadedAsync(Arg.Any<string>()).Returns(Task.FromResult(true));

        var executionGuard = Substitute.For<IModuleExecutionGuard>();
        executionGuard.CanExecute(Arg.Any<string>()).Returns(true);
        executionGuard.ExecuteSafe<object?>(
            Arg.Any<string>(),
            Arg.Any<Func<object?>>(),
            Arg.Any<object?>(),
            Arg.Any<string>())
            .Returns(ci => ci.ArgAt<Func<object?>>(1)());

        var uiFactory = Substitute.For<IUIFactory>();
        uiFactory.CreateView(Arg.Any<object>()).Returns(new object());

        var logger = Substitute.For<ILogger<AvaloniaNavigationService>>();

        var nav = new AvaloniaNavigationService(
            hostProvider,
            uiFactory,
            menuRegistry,
            runtimeContext,
            lazyLoader,
            executionGuard,
            logger);

        var guard = new RecordingGuard(calls);
        nav.RegisterNavigationGuard(guard);

        // 1) First navigation establishes current VM (A) - we don't assert order here.
        calls.Clear();
        Assert.True(await nav.NavigateToAsync(menuA.Id));

        // 2) Navigate A -> B, assert order.
        calls.Clear();
        Assert.True(await nav.NavigateToAsync(menuB.Id));

        Assert.Equal(
            new[]
            {
                "guard:from",
                "guard:to",
                "vmA:canFrom",
                "vmB:canTo",
                "vmA:from",
                "vmB:to"
            },
            calls);
    }

    [Fact]
    public async Task Navigate_CurrentVM_CanNavigateFromFalse_ShortCircuits_Before_TargetVM()
    {
        var calls = new List<string>();

        var moduleId = "TestModule";

        var services = new ServiceCollection();
        services.AddSingleton(new CallRecorder(calls));
        services.AddTransient<BlockingVm>();
        services.AddTransient<TestVmB>();
        var hostProvider = services.BuildServiceProvider();
        var runtimeContext = CreateRuntimeContextWithModuleHandle(moduleId, typeof(BlockingVm).Assembly, hostProvider);

        var menuA = new MenuItem($"{moduleId}.avalonia.block.0", "Block", IconKind.Home, typeof(BlockingVm).FullName!);
        menuA.ModuleId = moduleId;
        var menuB = new MenuItem($"{moduleId}.avalonia.b.0", "B", IconKind.Grid, typeof(TestVmB).FullName!);
        menuB.ModuleId = moduleId;

        var menuRegistry = Substitute.For<IMenuRegistry>();
        menuRegistry.GetItems(MenuLocation.Main).Returns(new[] { menuA, menuB });
        menuRegistry.GetItems(MenuLocation.Bottom).Returns(Array.Empty<MenuItem>());

        var lazyLoader = Substitute.For<ILazyModuleLoader>();
        lazyLoader.EnsureModuleLoadedAsync(Arg.Any<string>()).Returns(Task.FromResult(true));

        var executionGuard = Substitute.For<IModuleExecutionGuard>();
        executionGuard.CanExecute(Arg.Any<string>()).Returns(true);
        executionGuard.ExecuteSafe<object?>(
                Arg.Any<string>(),
                Arg.Any<Func<object?>>(),
                Arg.Any<object?>(),
                Arg.Any<string>())
            .Returns(ci => ci.ArgAt<Func<object?>>(1)());

        var uiFactory = Substitute.For<IUIFactory>();
        uiFactory.CreateView(Arg.Any<object>()).Returns(new object());

        var logger = Substitute.For<ILogger<AvaloniaNavigationService>>();

        var nav = new AvaloniaNavigationService(
            hostProvider,
            uiFactory,
            menuRegistry,
            runtimeContext,
            lazyLoader,
            executionGuard,
            logger);

        nav.RegisterNavigationGuard(new RecordingGuard(calls));

        // Establish current VM = BlockingVm
        calls.Clear();
        Assert.True(await nav.NavigateToAsync(menuA.Id));

        // Attempt navigate to B - should be blocked by current VM.
        calls.Clear();
        Assert.False(await nav.NavigateToAsync(menuB.Id));

        Assert.Equal(
            new[]
            {
                "guard:from",
                "guard:to",
                "block:canFrom"
            },
            calls);
    }

    private static RuntimeContext CreateRuntimeContextWithModuleHandle(string moduleId, System.Reflection.Assembly moduleAssembly, IServiceProvider moduleServiceProvider)
    {
        var runtimeContext = new RuntimeContext();

        var sharedCatalog = Substitute.For<ISharedAssemblyCatalog>();
        sharedCatalog.IsShared(Arg.Any<System.Reflection.AssemblyName>()).Returns(false);
        sharedCatalog.Names.Returns(Array.Empty<string>());
        sharedCatalog.GetEntries().Returns(Array.Empty<SharedAssemblyEntry>());
        sharedCatalog.GetMismatches().Returns(Array.Empty<SharedAssemblyMismatch>());
        sharedCatalog.AddManifestHints(Arg.Any<string>(), Arg.Any<IEnumerable<string>>()).Returns(Array.Empty<SharedAssemblyMismatch>());

        var loadContext = new ModuleLoadContext(moduleId, basePath: System.IO.Path.GetTempPath(), sharedCatalog);
        var descriptor = new ModuleDescriptor(moduleId, "1.0.0");
        var manifest = new VsixManifest
        {
            Metadata = new ManifestMetadata
            {
                Identity = new ManifestIdentity { Id = moduleId, Version = "1.0.0", Publisher = "Test" },
                DisplayName = moduleId
            }
        };

        var runtimeModule = new RuntimeModule(descriptor, loadContext, packagePath: System.IO.Path.GetTempPath(), manifest);
        runtimeContext.RegisterModule(runtimeModule);

        var handle = new RuntimeModuleHandle(
            runtimeModule,
            manifest,
            serviceScope: null,
            serviceProvider: moduleServiceProvider,
            compositeServiceProvider: moduleServiceProvider,
            moduleInstances: Array.Empty<IModule>(),
            registeredMenus: Array.Empty<MenuItem>(),
            assemblies: new[] { moduleAssembly });

        runtimeContext.RegisterModuleHandle(handle);
        return runtimeContext;
    }

    private sealed class RecordingGuard : INavigationGuard
    {
        private readonly List<string> _calls;
        public RecordingGuard(List<string> calls) => _calls = calls;

        public Task<bool> CanNavigateFromAsync(NavigationContext context)
        {
            _calls.Add("guard:from");
            return Task.FromResult(true);
        }

        public Task<bool> CanNavigateToAsync(NavigationContext context)
        {
            _calls.Add("guard:to");
            return Task.FromResult(true);
        }
    }

    private sealed class CallRecorder
    {
        public CallRecorder(List<string> calls) => Calls = calls;
        public List<string> Calls { get; }
    }

    private sealed class TestVmA : ViewModelBase
    {
        private readonly CallRecorder _rec;
        public TestVmA(CallRecorder rec) => _rec = rec;

        public override Task<bool> CanNavigateFromAsync(NavigationContext context)
        {
            _rec.Calls.Add("vmA:canFrom");
            return Task.FromResult(true);
        }

        public override Task OnNavigatedFromAsync(NavigationContext context)
        {
            _rec.Calls.Add("vmA:from");
            return Task.CompletedTask;
        }
    }

    private sealed class TestVmB : ViewModelBase
    {
        private readonly CallRecorder _rec;
        public TestVmB(CallRecorder rec) => _rec = rec;

        public override Task<bool> CanNavigateToAsync(NavigationContext context)
        {
            _rec.Calls.Add("vmB:canTo");
            return Task.FromResult(true);
        }

        public override Task OnNavigatedToAsync(NavigationContext context)
        {
            _rec.Calls.Add("vmB:to");
            return Task.CompletedTask;
        }
    }

    private sealed class BlockingVm : ViewModelBase
    {
        private readonly CallRecorder _rec;
        public BlockingVm(CallRecorder rec) => _rec = rec;

        public override Task<bool> CanNavigateFromAsync(NavigationContext context)
        {
            _rec.Calls.Add("block:canFrom");
            return Task.FromResult(false);
        }
    }
}


