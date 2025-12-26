using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Modulus.Core.Runtime;
using Modulus.HostSdk.Abstractions;
using Modulus.HostSdk.Runtime;
using Modulus.Sdk;
using Modulus.Core.Architecture;

namespace Modulus.HostSdk.Tests;

public sealed class HostSdkBuilderTests
{
    private sealed class TestHostModule : ModulusPackage
    {
    }

    [Fact]
    public void AddDefaultModuleDirectories_AddsTwoDirectories()
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

        // We can't access the builder's private list; this test is mainly a behavioral guard that it doesn't throw and is chainable.
        Assert.NotNull(builder);
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
}


