using Microsoft.Extensions.Logging;
using Modulus.Core.Architecture;
using Modulus.Core.Runtime;
using Modulus.Sdk;
using Modulus.UI.Abstractions;
using NSubstitute;

namespace Modulus.Core.Tests.Runtime;

public class ModuleStylesheetLoaderTests
{
    [Fact]
    public void TryLoadCssForRoute_WhenModuleCssExists_ReturnsCssWithSignal()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "ModulusTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);
        try
        {
            File.WriteAllText(Path.Combine(tempRoot, "module.css"), ":root{--signal:1;}");

            var menuRegistry = Substitute.For<IMenuRegistry>();
            menuRegistry.GetItems(MenuLocation.Main).Returns(new[]
            {
                new MenuItem("m1", "Home", IconKind.Home, "/home", MenuLocation.Main, 1)
                {
                    ModuleId = "module-1"
                }
            });
            menuRegistry.GetItems(MenuLocation.Bottom).Returns(Array.Empty<MenuItem>());

            var runtimeContext = new RuntimeContext();
            runtimeContext.SetCurrentHost(ModulusHostIds.Blazor);

            var sharedCatalog = Substitute.For<ISharedAssemblyCatalog>();
            sharedCatalog.IsShared(Arg.Any<System.Reflection.AssemblyName>()).Returns(true);

            var descriptor = new ModuleDescriptor("module-1", "1.0.0", "M1", "Desc", [ModulusHostIds.Blazor]);
            var manifest = new VsixManifest
            {
                Metadata = new ManifestMetadata
                {
                    Identity = new ManifestIdentity { Id = "module-1", Version = "1.0.0", Publisher = "Test", Language = "en-US" },
                    DisplayName = "M1"
                }
            };
            var alc = new ModuleLoadContext("module-1", tempRoot, sharedCatalog, Substitute.For<ILogger>());
            var runtimeModule = new RuntimeModule(descriptor, alc, tempRoot, manifest, isSystem: true);

            var handle = new RuntimeModuleHandle(
                runtimeModule,
                manifest,
                serviceScope: null,
                serviceProvider: Substitute.For<IServiceProvider>(),
                compositeServiceProvider: Substitute.For<IServiceProvider>(),
                moduleInstances: Array.Empty<IModule>(),
                registeredMenus: Array.Empty<MenuItem>(),
                assemblies: Array.Empty<System.Reflection.Assembly>());

            runtimeContext.RegisterModule(runtimeModule);
            runtimeContext.RegisterModuleHandle(handle);

            var css = ModuleStylesheetLoader.TryLoadCssForRoute("/home", menuRegistry, runtimeContext);

            Assert.NotNull(css);
            Assert.Contains("--signal:1", css);
        }
        finally
        {
            try { Directory.Delete(tempRoot, true); } catch { /* ignore */ }
        }
    }
}


