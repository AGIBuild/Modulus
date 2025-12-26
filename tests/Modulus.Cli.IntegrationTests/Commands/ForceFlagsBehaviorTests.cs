using Modulus.Cli.IntegrationTests.Infrastructure;

namespace Modulus.Cli.IntegrationTests.Commands;

/// <summary>
/// Covers branches around --force behavior in handlers/commands.
/// </summary>
public class ForceFlagsBehaviorTests : IDisposable
{
    private readonly CliTestContext _context;
    private readonly CliRunner _runner;

    public ForceFlagsBehaviorTests()
    {
        _context = new CliTestContext();
        _runner = new CliRunner(_context);
    }

    [Fact]
    public async Task Install_WithoutForce_WhenAlreadyInstalled_Fails()
    {
        var artifact = await SharedCliArtifacts.GetAvaloniaAsync();

        var first = await _runner.InstallAsync(artifact.PackagePath, force: true);
        Assert.True(first.IsSuccess, first.CombinedOutput);

        var second = await _runner.InstallAsync(artifact.PackagePath, force: false);
        Assert.False(second.IsSuccess);
        Assert.Contains("already installed", second.CombinedOutput, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("--force", second.CombinedOutput, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Uninstall_WithoutForce_FailsWithConfirmationRequired()
    {
        var artifact = await SharedCliArtifacts.GetAvaloniaAsync();

        var install = await _runner.InstallAsync(artifact.PackagePath, force: true);
        Assert.True(install.IsSuccess, install.CombinedOutput);

        var uninstall = await _runner.UninstallAsync(artifact.ModuleName, force: false);
        Assert.False(uninstall.IsSuccess);
        Assert.Contains("confirmation", uninstall.CombinedOutput, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("--force", uninstall.CombinedOutput, StringComparison.OrdinalIgnoreCase);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}


