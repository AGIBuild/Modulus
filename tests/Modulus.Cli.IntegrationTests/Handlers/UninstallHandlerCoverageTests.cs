using Microsoft.Extensions.DependencyInjection;
using Modulus.Cli.Commands.Handlers;
using Modulus.Cli.Services;
using Modulus.Infrastructure.Data;
using Modulus.Infrastructure.Data.Models;

namespace Modulus.Cli.IntegrationTests.Handlers;

public class UninstallHandlerCoverageTests
{
    [Fact]
    public async Task UninstallHandler_DeletesModuleDirectory_WhenExists()
    {
        var workDir = Path.Combine(Path.GetTempPath(), $"modulus-uninstall-dir-{Guid.NewGuid():N}");
        Directory.CreateDirectory(workDir);
        var dbPath = Path.Combine(workDir, "test.db");
        var modulesDir = Path.Combine(workDir, "Modules");
        Directory.CreateDirectory(modulesDir);

        using var services = new ServiceCollection()
            .AddCliServices(verbose: false, databasePath: dbPath, modulesDirectory: modulesDir)
            .BuildServiceProvider();

        await CliServiceProvider.EnsureMigratedAsync(services);

        var id = Guid.NewGuid().ToString("D");
        var moduleDir = Path.Combine(modulesDir, id);
        Directory.CreateDirectory(moduleDir);
        File.WriteAllText(Path.Combine(moduleDir, "x.txt"), "x");

        var module = new ModuleEntity
        {
            Id = id,
            Version = "1.0.0",
            DisplayName = "DeleteMe",
            Path = Path.Combine(moduleDir, "extension.vsixmanifest"),
            IsSystem = false
        };

        var db = services.GetRequiredService<ModulusDbContext>();
        db.Modules.Add(module);
        await db.SaveChangesAsync();

        var handler = new UninstallHandler(services, modulesDir);
        var result = await handler.ExecuteAsync("DeleteMe", force: true, output: new StringWriter());

        Assert.True(result.Success, result.Message);
        Assert.False(Directory.Exists(moduleDir), "Expected module directory to be deleted.");

        try { Directory.Delete(workDir, recursive: true); } catch { /* ignore */ }
    }
}


