using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Modulus.Core.Runtime;
using Modulus.HostSdk.Abstractions;
using Modulus.HostSdk.Runtime;
using Modulus.Sdk;
using Modulus.Core.Architecture;
using Modulus.Core.Paths;
using System.Reflection;

namespace Modulus.HostSdk.Tests;

public sealed class HostSdkBuilderTests
{
    private sealed class TestHostModule : ModulusPackage
    {
    }

    [Fact]
    public void AddDefaultModuleDirectories_AddsExpectedDefaultDirectories()
    {
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder().Build();
        var options = new ModulusHostSdkOptions
        {
            HostId = ModulusHostIds.Avalonia,
            HostVersion = new Version(1, 0, 0),
            DatabasePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.db")
        };

        var builder = new ModulusHostSdkBuilder(services, config, options)
            .AddDefaultModuleDirectories()
            .AddModuleDirectory(Path.GetTempPath(), isSystem: false); // cover AddModuleDirectory

        Assert.True(builder.Options.ModuleDirectories.Count == 0, "Options.ModuleDirectories should not be mutated by builder methods.");

        // Validate defaults via reflection (avoid adding public API solely for tests).
        var list = GetPrivateModuleDirectories(builder);

        var systemDir = Path.Combine(AppContext.BaseDirectory, "Modules");
        var userDir = Path.Combine(LocalStorage.GetUserRoot(), "Modules");
        var legacyUserDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Modulus",
            "Modules");

        Assert.Contains(list, d => d.IsSystem && PathEquals(d.Path, systemDir));
        Assert.Contains(list, d => !d.IsSystem && PathEquals(d.Path, userDir));

        // Only included when distinct from the canonical user directory.
        if (!PathEquals(userDir, legacyUserDir))
        {
            Assert.Contains(list, d => !d.IsSystem && PathEquals(d.Path, legacyUserDir));
        }
    }

    [Fact]
    public void AddModuleDirectory_ThrowsForWhitespace()
    {
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder().Build();
        var options = new ModulusHostSdkOptions
        {
            HostId = ModulusHostIds.Avalonia,
            HostVersion = new Version(1, 0, 0),
            DatabasePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.db")
        };

        var builder = new ModulusHostSdkBuilder(services, config, options);
        Assert.Throws<ArgumentException>(() => builder.AddModuleDirectory("  ", isSystem: true));
    }

    [Fact]
    public void Constructor_ThrowsForNullArguments()
    {
        var config = new ConfigurationBuilder().Build();
        var options = new ModulusHostSdkOptions
        {
            HostId = ModulusHostIds.Avalonia,
            HostVersion = new Version(1, 0, 0),
            DatabasePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.db")
        };

        Assert.Throws<ArgumentNullException>(() => new ModulusHostSdkBuilder(null!, config, options));
        Assert.Throws<ArgumentNullException>(() => new ModulusHostSdkBuilder(new ServiceCollection(), null!, options));
        Assert.Throws<ArgumentNullException>(() => new ModulusHostSdkBuilder(new ServiceCollection(), config, null!));
    }

    [Fact]
    public async Task Constructor_CopiesModuleDirectoriesFromOptions()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"modulus-hostsdk-tests-moddir-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var services = new ServiceCollection();
            var config = new ConfigurationBuilder().Build();
            var options = new ModulusHostSdkOptions
            {
                HostId = ModulusHostIds.Avalonia,
                HostVersion = new Version(1, 0, 0),
                DatabasePath = Path.Combine(tempDir, "modulus.db"),
                ModuleDirectories = new List<HostModuleDirectory>
                {
                    new(tempDir, IsSystem: true)
                }
            };

            var builder = new ModulusHostSdkBuilder(services, config, options)
                .AddDefaultRuntimeServices();

            var app = await builder.BuildAsync<TestHostModule>(NullLoggerFactory.Instance);
            Assert.NotNull(app);
        }
        finally
        {
            try { Directory.Delete(tempDir, recursive: true); } catch { }
        }
    }

    [Fact]
    public void AddDefaultRuntimeServices_RegistersExpectedServices()
    {
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder().Build();
        var options = new ModulusHostSdkOptions
        {
            HostId = ModulusHostIds.Avalonia,
            HostVersion = new Version(1, 0, 0),
            DatabasePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.db")
        };

        var builder = new ModulusHostSdkBuilder(services, config, options)
            .AddDefaultRuntimeServices();

        // Sanity: key runtime services should be registered (registration check).
        // Note: Some services (e.g. ILazyModuleLoader) depend on IModuleLoader which is wired by ModulusApplicationFactory during BuildAsync.
        Assert.Contains(builder.Services, d => d.ServiceType == typeof(ILazyModuleLoader));
        Assert.NotNull(builder);
    }

    [Fact]
    public async Task BuildAsync_CreatesApplication()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"modulus-hostsdk-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            var dbPath = Path.Combine(tempDir, "modulus.db");

            var services = new ServiceCollection();
            var config = new ConfigurationBuilder().Build();
            var options = new ModulusHostSdkOptions
            {
                HostId = ModulusHostIds.Avalonia,
                HostVersion = new Version(1, 0, 0),
                DatabasePath = dbPath
            };

            var builder = new ModulusHostSdkBuilder(services, config, options)
                .AddDefaultRuntimeServices();

            var app = await builder.BuildAsync<TestHostModule>(NullLoggerFactory.Instance);

            Assert.NotNull(app);
            Assert.IsAssignableFrom<IModulusApplication>(app);
        }
        finally
        {
            try { Directory.Delete(tempDir, recursive: true); } catch { }
        }
    }

    private static List<HostModuleDirectory> GetPrivateModuleDirectories(ModulusHostSdkBuilder builder)
    {
        var field = typeof(ModulusHostSdkBuilder).GetField("_moduleDirectories", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);

        var value = field!.GetValue(builder);
        Assert.NotNull(value);

        return Assert.IsAssignableFrom<List<HostModuleDirectory>>(value);
    }

    private static bool PathEquals(string a, string b)
    {
        var comparer = OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
        return comparer.Equals(Normalize(a), Normalize(b));
    }

    private static string Normalize(string path)
    {
        var full = Path.GetFullPath(path);
        return full.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }
}


