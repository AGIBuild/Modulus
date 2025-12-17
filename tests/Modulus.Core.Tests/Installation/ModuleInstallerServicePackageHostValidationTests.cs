using System.IO.Compression;
using Microsoft.Extensions.Logging;
using Modulus.Core.Installation;
using Modulus.Core.Manifest;
using Modulus.Infrastructure.Data.Repositories;
using Modulus.Sdk;
using NSubstitute;

namespace Modulus.Core.Tests.Installation;

public class ModuleInstallerServicePackageHostValidationTests
{
    [Fact]
    public async Task InstallFromPackageAsync_WhenHostNotSupported_ReturnsFailed_AndDoesNotWriteDb()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "ModulusTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);

        var packageDir = Path.Combine(tempRoot, "pkg");
        Directory.CreateDirectory(packageDir);

        // Create a minimal package layout: manifest + one dll (core)
        File.WriteAllBytes(Path.Combine(packageDir, "Test.Core.dll"), Array.Empty<byte>());
        WriteManifest(
            packageDir,
            moduleId: Guid.NewGuid().ToString(),
            supportedHostId: ModulusHostIds.Avalonia,
            hostSpecificUiDllName: "Test.UI.Avalonia.dll",
            hostSpecificUiHostId: ModulusHostIds.Avalonia);
        File.WriteAllBytes(Path.Combine(packageDir, "Test.UI.Avalonia.dll"), Array.Empty<byte>());

        var modpkgPath = Path.Combine(tempRoot, "Test.modpkg");
        ZipFile.CreateFromDirectory(packageDir, modpkgPath);

        var moduleRepo = Substitute.For<IModuleRepository>();
        var menuRepo = Substitute.For<IMenuRepository>();
        var cleanup = Substitute.For<IModuleCleanupService>();
        var logger = Substitute.For<ILogger<ModuleInstallerService>>();
        var validator = new DefaultManifestValidator(Substitute.For<ILogger<DefaultManifestValidator>>());

        var sut = new ModuleInstallerService(moduleRepo, menuRepo, validator, cleanup, logger);

        var result = await sut.InstallFromPackageAsync(modpkgPath, overwrite: false, hostType: ModulusHostIds.Blazor);

        Assert.False(result.Success);
        Assert.False(result.RequiresConfirmation);
        Assert.NotNull(result.Error);
        Assert.Contains("not compatible", result.Error!, StringComparison.OrdinalIgnoreCase);

        await moduleRepo.DidNotReceiveWithAnyArgs().UpsertAsync(default!, default);
        await menuRepo.DidNotReceiveWithAnyArgs().ReplaceModuleMenusAsync(default!, default!, default);

        try { Directory.Delete(tempRoot, true); } catch { /* ignore */ }
    }

    private static void WriteManifest(
        string moduleDir,
        string moduleId,
        string supportedHostId,
        string hostSpecificUiDllName,
        string hostSpecificUiHostId)
    {
        var manifest =
            $@"<?xml version=""1.0"" encoding=""utf-8""?>
<PackageManifest Version=""2.0.0"" xmlns=""http://schemas.microsoft.com/developer/vsx-schema/2011"">
  <Metadata>
    <Identity Id=""{moduleId}"" Version=""1.0.0"" Language=""en-US"" Publisher=""Test"" />
    <DisplayName>Test Module</DisplayName>
    <Description>Test</Description>
  </Metadata>

  <Installation>
    <InstallationTarget Id=""{supportedHostId}"" Version=""[1.0,)"" />
  </Installation>

  <Assets>
    <Asset Type=""{ModulusAssetTypes.Package}"" Path=""Test.Core.dll"" />
    <Asset Type=""{ModulusAssetTypes.Package}"" Path=""{hostSpecificUiDllName}"" TargetHost=""{hostSpecificUiHostId}"" />
  </Assets>
</PackageManifest>";

        File.WriteAllText(Path.Combine(moduleDir, SystemModuleInstaller.VsixManifestFileName), manifest);
    }
}


