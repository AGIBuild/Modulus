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
        // Arrange - Create, pack and install a module
        var newResult = await _runner.NewAsync("UninstallByName", "avalonia");
        Assert.True(newResult.IsSuccess, $"Failed to create module: {newResult.CombinedOutput}");
        
        var moduleDir = Path.Combine(_context.WorkingDirectory, "UninstallByName");
        var packResult = await _runner.PackAsync(path: moduleDir, output: _context.OutputDirectory);
        Assert.True(packResult.IsSuccess, $"Pack failed: {packResult.CombinedOutput}");
        
        var packages = Directory.GetFiles(_context.OutputDirectory, "*.modpkg");
        var installResult = await _runner.InstallAsync(packages[0], force: true);
        Assert.True(installResult.IsSuccess, $"Install failed: {installResult.CombinedOutput}");
        
        // Act - Uninstall by display name
        var result = await _runner.UninstallAsync("UninstallByName", force: true);
        
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
        // Arrange - Install a module
        var newResult = await _runner.NewAsync("ListThenUninstall", "avalonia");
        Assert.True(newResult.IsSuccess, $"Failed to create module: {newResult.CombinedOutput}");
        
        var moduleDir = Path.Combine(_context.WorkingDirectory, "ListThenUninstall");
        var packResult = await _runner.PackAsync(path: moduleDir, output: _context.OutputDirectory);
        Assert.True(packResult.IsSuccess, $"Pack failed: {packResult.CombinedOutput}");
        
        var packages = Directory.GetFiles(_context.OutputDirectory, "*.modpkg");
        var installResult = await _runner.InstallAsync(packages[0], force: true);
        Assert.True(installResult.IsSuccess, $"Install failed: {installResult.CombinedOutput}");
        
        // Verify module is in list
        var listBefore = await _runner.ListAsync();
        Assert.True(listBefore.IsSuccess);
        Assert.Contains("ListThenUninstall", listBefore.StandardOutput);
        
        // Act - Uninstall
        var uninstallResult = await _runner.UninstallAsync("ListThenUninstall", force: true);
        Assert.True(uninstallResult.IsSuccess, $"Uninstall failed: {uninstallResult.CombinedOutput}");
        
        // Assert - Module should not be in list anymore
        var listAfter = await _runner.ListAsync();
        Assert.True(listAfter.IsSuccess);
        Assert.DoesNotContain("ListThenUninstall", listAfter.StandardOutput);
    }
    
    public void Dispose()
    {
        _context.Dispose();
    }
}


