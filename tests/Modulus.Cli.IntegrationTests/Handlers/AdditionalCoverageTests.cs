using Microsoft.Extensions.DependencyInjection;
using Modulus.Cli.Commands.Handlers;
using Modulus.Cli.IntegrationTests.Infrastructure;
using Modulus.Cli.Services;
using Modulus.Infrastructure.Data;
using Modulus.Infrastructure.Data.Models;

namespace Modulus.Cli.IntegrationTests.Handlers;

public class AdditionalCoverageTests
{
    [Fact]
    public void CliServiceProvider_Build_CreatesConfiguration()
    {
        using var sp = CliServiceProvider.Build(verbose: false, databasePath: Path.GetTempFileName(), modulesDirectory: Path.GetTempPath());
        var cfg = sp.GetRequiredService<CliConfiguration>();
        Assert.False(string.IsNullOrWhiteSpace(cfg.ModulesDirectory));
    }

    [Fact]
    public async Task BuildHandler_FallbacksToFirstCsproj_WhenNoSdkReference()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"modulus-projloc-fallback-{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "A.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\"></Project>");

        try
        {
            var stdout = new StringWriter();
            var console = new TestCliConsole(@out: stdout);
            var runner = new FakeProcessRunner(req =>
            {
                Assert.Contains("A.csproj", req.Arguments, StringComparison.OrdinalIgnoreCase);
                return new ProcessRunResult(0, "", "");
            });

            var handler = new BuildHandler(console, runner);
            var code = await handler.ExecuteAsync(dir, "Release", verbose: false, CancellationToken.None);

            Assert.Equal(0, code);
            Assert.Contains("Build succeeded", stdout.ToString());
        }
        finally
        {
            try { Directory.Delete(dir, recursive: true); } catch { /* ignore */ }
        }
    }

    [Fact]
    public async Task BuildHandler_PicksCsprojWithModulusSdkReference()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"modulus-projloc-sdk-{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "A.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\"></Project>");
        File.WriteAllText(Path.Combine(dir, "B.csproj"),
            "<Project Sdk=\"Microsoft.NET.Sdk\"><ItemGroup><PackageReference Include=\"Modulus.Sdk\" Version=\"1.0.0\" /></ItemGroup></Project>");

        try
        {
            // Also ensure output dir exists so FindOutputDirectory hits a simple candidate.
            Directory.CreateDirectory(Path.Combine(dir, "bin", "Release", "net10.0"));

            var stdout = new StringWriter();
            var console = new TestCliConsole(@out: stdout);
            var runner = new FakeProcessRunner(req =>
            {
                Assert.Contains("B.csproj", req.Arguments, StringComparison.OrdinalIgnoreCase);
                return new ProcessRunResult(0, "", "");
            });

            var handler = new BuildHandler(console, runner);
            var code = await handler.ExecuteAsync(dir, "Release", verbose: false, CancellationToken.None);

            Assert.Equal(0, code);
            Assert.Contains("Build succeeded", stdout.ToString());
        }
        finally
        {
            try { Directory.Delete(dir, recursive: true); } catch { /* ignore */ }
        }
    }

    [Fact]
    public async Task BuildHandler_PicksCsprojWithAgibuildModulusSdkReference()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"modulus-projloc-agibuildsdk-{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "A.csproj"),
            "<Project Sdk=\"Microsoft.NET.Sdk\"><ItemGroup><PackageReference Include=\"Agibuild.Modulus.Sdk\" Version=\"1.0.0\" /></ItemGroup></Project>");

        try
        {
            // Ensure TFM output exists; ProjectLocator should now prefer net10.0 folder.
            Directory.CreateDirectory(Path.Combine(dir, "bin", "Release", "net10.0"));

            var stdout = new StringWriter();
            var console = new TestCliConsole(@out: stdout);
            var runner = new FakeProcessRunner(req =>
            {
                Assert.Contains("A.csproj", req.Arguments, StringComparison.OrdinalIgnoreCase);
                return new ProcessRunResult(0, "", "");
            });

            var handler = new BuildHandler(console, runner);
            var code = await handler.ExecuteAsync(dir, "Release", verbose: false, CancellationToken.None);

            Assert.Equal(0, code);
            Assert.Contains("Output: ", stdout.ToString());
            Assert.Contains("net10.0", stdout.ToString(), StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            try { Directory.Delete(dir, recursive: true); } catch { /* ignore */ }
        }
    }

    [Fact]
    public async Task ListHandler_Verbose_PrintsOptionalFieldsAndTags()
    {
        var workDir = Path.Combine(Path.GetTempPath(), $"modulus-listhandler-verbose-{Guid.NewGuid():N}");
        Directory.CreateDirectory(workDir);
        var dbPath = Path.Combine(workDir, "test.db");
        var modulesDir = Path.Combine(workDir, "Modules");
        Directory.CreateDirectory(modulesDir);

        using var services = new ServiceCollection()
            .AddCliServices(verbose: false, databasePath: dbPath, modulesDirectory: modulesDir)
            .BuildServiceProvider();

        await CliServiceProvider.EnsureMigratedAsync(services);

        var module = new ModuleEntity
        {
            Id = Guid.NewGuid().ToString("D"),
            Version = "2.0.0",
            DisplayName = "VerboseModule",
            Publisher = "PublisherX",
            Description = "Desc",
            Path = Path.Combine(modulesDir, "VerboseModule", "extension.vsixmanifest"),
            IsEnabled = false,
            IsSystem = true,
            State = Modulus.Infrastructure.Data.Models.ModuleState.MissingFiles,
            ValidatedAt = new DateTime(2025, 1, 2, 3, 4, 5, DateTimeKind.Utc)
        };

        var db = services.GetRequiredService<ModulusDbContext>();
        db.Modules.Add(module);
        await db.SaveChangesAsync();

        var output = new StringWriter();
        var handler = new ListHandler(services);
        var result = await handler.ExecuteAsync(verbose: true, output: output);

        Assert.True(result.Success, result.Message);
        var text = output.ToString();
        Assert.Contains("Installed modules", text);
        Assert.Contains("VerboseModule", text);
        Assert.Contains("Publisher:", text);
        Assert.Contains("Description:", text);
        Assert.Contains("Path:", text);
        Assert.Contains("[system]", text);
        Assert.Contains("[MissingFiles]", text);

        try { Directory.Delete(workDir, recursive: true); } catch { /* ignore */ }
    }

    [Fact]
    public async Task UninstallHandler_PreventsUninstallingSystemModule()
    {
        var workDir = Path.Combine(Path.GetTempPath(), $"modulus-uninstall-system-{Guid.NewGuid():N}");
        Directory.CreateDirectory(workDir);
        var dbPath = Path.Combine(workDir, "test.db");
        var modulesDir = Path.Combine(workDir, "Modules");
        Directory.CreateDirectory(modulesDir);

        using var services = new ServiceCollection()
            .AddCliServices(verbose: false, databasePath: dbPath, modulesDirectory: modulesDir)
            .BuildServiceProvider();

        await CliServiceProvider.EnsureMigratedAsync(services);

        var module = new ModuleEntity
        {
            Id = Guid.NewGuid().ToString("D"),
            Version = "1.0.0",
            DisplayName = "SystemModule",
            Path = Path.Combine(modulesDir, "system", "extension.vsixmanifest"),
            IsSystem = true
        };

        var db = services.GetRequiredService<ModulusDbContext>();
        db.Modules.Add(module);
        await db.SaveChangesAsync();

        var handler = new UninstallHandler(services, modulesDir);
        var result = await handler.ExecuteAsync("SystemModule", force: true, output: new StringWriter());

        Assert.False(result.Success);
        Assert.Contains("system", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UninstallHandler_DeletesFromStoredPath_WhenModuleDirMissing()
    {
        var workDir = Path.Combine(Path.GetTempPath(), $"modulus-uninstall-path-{Guid.NewGuid():N}");
        Directory.CreateDirectory(workDir);
        var dbPath = Path.Combine(workDir, "test.db");
        var modulesDir = Path.Combine(workDir, "Modules");
        Directory.CreateDirectory(modulesDir);

        // Create a directory that will be deleted via moduleEntity.Path.
        var storedDir = Path.Combine(workDir, "StoredPathModule");
        Directory.CreateDirectory(storedDir);
        var storedManifest = Path.Combine(storedDir, "extension.vsixmanifest");
        File.WriteAllText(storedManifest, "<x/>");

        using var services = new ServiceCollection()
            .AddCliServices(verbose: false, databasePath: dbPath, modulesDirectory: modulesDir)
            .BuildServiceProvider();

        await CliServiceProvider.EnsureMigratedAsync(services);

        var id = Guid.NewGuid().ToString("D");
        var module = new ModuleEntity
        {
            Id = id,
            Version = "1.0.0",
            DisplayName = "PathFallback",
            Path = storedManifest,
            IsSystem = false
        };

        var db = services.GetRequiredService<ModulusDbContext>();
        db.Modules.Add(module);
        await db.SaveChangesAsync();

        // Ensure module directory under modules root does not exist so fallback path is taken.
        var expectedModulesDir = Path.Combine(modulesDir, id);
        if (Directory.Exists(expectedModulesDir))
        {
            Directory.Delete(expectedModulesDir, recursive: true);
        }

        var handler = new UninstallHandler(services, modulesDir);
        var result = await handler.ExecuteAsync("PathFallback", force: true, output: new StringWriter());

        Assert.True(result.Success, result.Message);
        Assert.False(Directory.Exists(storedDir), "Expected stored-path directory to be deleted.");
    }
}


