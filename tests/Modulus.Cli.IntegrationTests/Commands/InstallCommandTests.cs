using Modulus.Cli.IntegrationTests.Infrastructure;

namespace Modulus.Cli.IntegrationTests.Commands;

/// <summary>
/// Integration tests for 'modulus install' command.
/// Note: These tests require database access for module registration.
/// </summary>
public class InstallCommandTests : IDisposable
{
    private readonly CliTestContext _context;
    private readonly CliRunner _runner;
    
    public InstallCommandTests()
    {
        _context = new CliTestContext();
        _runner = new CliRunner(_context);
    }
    
    [Fact]
    public async Task Install_FromModpkg_Succeeds()
    {
        // Arrange - Create and pack a module
        var newResult = await _runner.NewAsync("InstallTest", "avalonia");
        Assert.True(newResult.IsSuccess, $"Failed to create module: {newResult.CombinedOutput}");
        
        var moduleDir = Path.Combine(_context.WorkingDirectory, "InstallTest");
        var packResult = await _runner.PackAsync(path: moduleDir, output: _context.OutputDirectory);
        Assert.True(packResult.IsSuccess, $"Pack failed: {packResult.CombinedOutput}");
        
        var packages = Directory.GetFiles(_context.OutputDirectory, "*.modpkg");
        Assert.Single(packages);
        
        // Act
        var result = await _runner.InstallAsync(packages[0], force: true);
        
        // Assert
        Assert.True(result.IsSuccess, $"Install failed: {result.CombinedOutput}");
        Assert.Contains("installed successfully", result.StandardOutput);
    }
    
    [Fact]
    public async Task Install_FromDirectory_Succeeds()
    {
        // Arrange - Create and build a module (not packed)
        var newResult = await _runner.NewAsync("DirInstall", "avalonia");
        Assert.True(newResult.IsSuccess, $"Failed to create module: {newResult.CombinedOutput}");
        
        var moduleDir = Path.Combine(_context.WorkingDirectory, "DirInstall");
        var buildResult = await _runner.BuildAsync(path: moduleDir);
        Assert.True(buildResult.IsSuccess, $"Build failed: {buildResult.CombinedOutput}");
        
        // Act - Install from module directory
        var result = await _runner.InstallAsync(moduleDir, force: true);
        
        // Assert
        Assert.True(result.IsSuccess, $"Install failed: {result.CombinedOutput}");
        Assert.Contains("installed successfully", result.StandardOutput);
    }
    
    [Fact]
    public async Task Install_ForceOverwrite_Succeeds()
    {
        // Arrange - Create and install a module
        var newResult = await _runner.NewAsync("ForceInstall", "avalonia");
        Assert.True(newResult.IsSuccess, $"Failed to create module: {newResult.CombinedOutput}");
        
        var moduleDir = Path.Combine(_context.WorkingDirectory, "ForceInstall");
        var packResult = await _runner.PackAsync(path: moduleDir, output: _context.OutputDirectory);
        Assert.True(packResult.IsSuccess, $"Pack failed: {packResult.CombinedOutput}");
        
        var packages = Directory.GetFiles(_context.OutputDirectory, "*.modpkg");
        
        // Install first time
        var firstInstall = await _runner.InstallAsync(packages[0], force: true);
        Assert.True(firstInstall.IsSuccess, $"First install failed: {firstInstall.CombinedOutput}");
        
        // Act - Install again with force
        var result = await _runner.InstallAsync(packages[0], force: true);
        
        // Assert
        Assert.True(result.IsSuccess, $"Second install failed: {result.CombinedOutput}");
        Assert.Contains("installed successfully", result.StandardOutput);
    }
    
    [Fact]
    public async Task Install_NonExistentPath_Fails()
    {
        // Act
        var result = await _runner.InstallAsync("/nonexistent/path.modpkg", force: true);
        
        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("not found", result.CombinedOutput.ToLowerInvariant());
    }
    
    [Fact]
    public async Task Install_InvalidPackage_Fails()
    {
        // Arrange - Create invalid .modpkg file
        var invalidPkg = _context.CreateFile("invalid.modpkg", "not a valid zip");
        
        // Act
        var result = await _runner.InstallAsync(invalidPkg, force: true);
        
        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("extract", result.CombinedOutput.ToLowerInvariant());
    }
    
    public void Dispose()
    {
        _context.Dispose();
    }
}


