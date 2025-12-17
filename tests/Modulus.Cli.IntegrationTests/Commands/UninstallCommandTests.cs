using Modulus.Cli.IntegrationTests.Infrastructure;

namespace Modulus.Cli.IntegrationTests.Commands;

/// <summary>
/// Integration tests for 'modulus uninstall' command.
/// </summary>
public class UninstallCommandTests : IDisposable
{
    private readonly CliTestContext _context;
    private readonly CliRunner _runner;
    
    public UninstallCommandTests()
    {
        _context = new CliTestContext();
        _runner = new CliRunner(_context);
    }
    
    [Fact]
    public async Task Uninstall_ByName_Succeeds()
    {
        // Arrange - reuse shared prebuilt package
        var artifact = await SharedCliArtifacts.GetAvaloniaAsync();
        var installResult = await _runner.InstallAsync(artifact.PackagePath, force: true);
        Assert.True(installResult.IsSuccess, $"Install failed: {installResult.CombinedOutput}");
        
        // Act - Uninstall by display name
        var result = await _runner.UninstallAsync(artifact.ModuleName, force: true);
        
        // Assert
        Assert.True(result.IsSuccess, $"Uninstall failed: {result.CombinedOutput}");
        Assert.Contains("uninstalled successfully", result.StandardOutput);
    }
    
    [Fact]
    public async Task Uninstall_NonExistentModule_Fails()
    {
        // Act
        var result = await _runner.UninstallAsync("non-existent-module", force: true);
        
        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("not found", result.CombinedOutput.ToLowerInvariant());
    }
    
    [Fact]
    public async Task Uninstall_AfterListConfirmsPresence()
    {
        // Arrange - reuse shared prebuilt package
        var artifact = await SharedCliArtifacts.GetAvaloniaAsync();
        var installResult = await _runner.InstallAsync(artifact.PackagePath, force: true);
        Assert.True(installResult.IsSuccess, $"Install failed: {installResult.CombinedOutput}");
        
        // Verify module is in list
        var listBefore = await _runner.ListAsync();
        Assert.True(listBefore.IsSuccess);
        Assert.Contains(artifact.ModuleName, listBefore.StandardOutput);
        
        // Act - Uninstall
        var uninstallResult = await _runner.UninstallAsync(artifact.ModuleName, force: true);
        Assert.True(uninstallResult.IsSuccess, $"Uninstall failed: {uninstallResult.CombinedOutput}");
        
        // Assert - Module should not be in list anymore
        var listAfter = await _runner.ListAsync();
        Assert.True(listAfter.IsSuccess);
        Assert.DoesNotContain(artifact.ModuleName, listAfter.StandardOutput);
    }
    
    public void Dispose()
    {
        _context.Dispose();
    }
}


