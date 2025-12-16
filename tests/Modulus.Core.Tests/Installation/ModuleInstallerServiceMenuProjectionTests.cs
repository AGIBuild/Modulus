using System.Reflection;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using Modulus.Core.Installation;
using Modulus.Core.Manifest;
using Modulus.Infrastructure.Data.Models;
using Modulus.Infrastructure.Data.Repositories;
using Modulus.Sdk;
using NSubstitute;

namespace Modulus.Core.Tests.Installation;

public class ModuleInstallerServiceMenuProjectionTests
{
    [Fact]
    public async Task InstallFromPathAsync_BlazorHost_ProjectsMenusFromAttributes_AndPreservesIsEnabled()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "ModulusTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);
        try
        {
            var moduleId = Guid.NewGuid().ToString();
            var moduleDir = Path.Combine(tempRoot, "TestModule");
            Directory.CreateDirectory(moduleDir);

            // Copy this test assembly as the host-specific UI package to parse menu attributes.
            var asmPath = Assembly.GetExecutingAssembly().Location;
            var uiDllName = "Test.UI.Blazor.dll";
            File.Copy(asmPath, Path.Combine(moduleDir, uiDllName));

            // Write minimal manifest
            WriteManifest(moduleDir, moduleId, uiDllName, ModulusHostIds.Blazor);

            var moduleRepo = Substitute.For<IModuleRepository>();
            var menuRepo = Substitute.For<IMenuRepository>();
            var validator = Substitute.For<IManifestValidator>();
            var cleanup = Substitute.For<IModuleCleanupService>();
            var logger = Substitute.For<ILogger<ModuleInstallerService>>();

            // Existing module disabled -> MUST remain disabled
            moduleRepo.GetAsync(moduleId, Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<ModuleEntity?>(new ModuleEntity { Id = moduleId, IsEnabled = false }));

            validator.ValidateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<VsixManifest>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(ManifestValidationResult.Success()));

            var sut = new ModuleInstallerService(moduleRepo, menuRepo, validator, cleanup, logger);

            await sut.InstallFromPathAsync(moduleDir, isSystem: true, hostType: ModulusHostIds.Blazor);

            await moduleRepo.Received(1).UpsertAsync(
                Arg.Is<ModuleEntity>(m => m.Id == moduleId && m.IsEnabled == false),
                Arg.Any<CancellationToken>());

            await menuRepo.Received(1).ReplaceModuleMenusAsync(
                moduleId,
                Arg.Is<IEnumerable<MenuEntity>>(menus => menus.Any(me => me.Route == "/test-blazor")),
                Arg.Any<CancellationToken>());
        }
        finally
        {
            try { Directory.Delete(tempRoot, true); } catch { /* ignore */ }
        }
    }

    private static void WriteManifest(string moduleDir, string moduleId, string uiDllName, string hostId)
    {
        XNamespace ns = "http://schemas.microsoft.com/developer/vsx-schema/2011";

        var doc = new XDocument(
            new XElement(ns + "PackageManifest",
                new XAttribute("Version", "2.0.0"),
                new XElement(ns + "Metadata",
                    new XElement(ns + "Identity",
                        new XAttribute("Id", moduleId),
                        new XAttribute("Version", "1.0.0"),
                        new XAttribute("Language", "en-US"),
                        new XAttribute("Publisher", "Test")),
                    new XElement(ns + "DisplayName", "Test Module"),
                    new XElement(ns + "Description", "Test")),
                new XElement(ns + "Installation",
                    new XElement(ns + "InstallationTarget", new XAttribute("Id", hostId), new XAttribute("Version", "[1.0,)"))),
                new XElement(ns + "Assets",
                    new XElement(ns + "Asset",
                        new XAttribute("Type", ModulusAssetTypes.Package),
                        new XAttribute("Path", uiDllName),
                        new XAttribute("TargetHost", hostId)))));

        doc.Save(Path.Combine(moduleDir, SystemModuleInstaller.VsixManifestFileName));
    }
}


