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
        // Arrange - reuse shared prebuilt package to avoid repeated new/pack/build cost
        var artifact = await SharedCliArtifacts.GetAvaloniaAsync();
        
        // Act
        var result = await _runner.InstallAsync(artifact.PackagePath, force: true);
        
        // Assert
        Assert.True(result.IsSuccess, $"Install failed: {result.CombinedOutput}");
        Assert.Contains("installed successfully", result.StandardOutput);
    }
    
    [Fact]
    public async Task Install_FromDirectory_Succeeds()
    {
        // Arrange - reuse extracted directory from shared package (valid manifest + DLLs present)
        var artifact = await SharedCliArtifacts.GetAvaloniaAsync();
        
        // Act - Install from module directory
        var result = await _runner.InstallAsync(artifact.ExtractedDir, force: true);
        
        // Assert
        Assert.True(result.IsSuccess, $"Install failed: {result.CombinedOutput}");
        Assert.Contains("installed successfully", result.StandardOutput);
    }
    
    [Fact]
    public async Task Install_ForceOverwrite_Succeeds()
    {
        // Arrange - install the same shared package twice (force overwrite)
        var artifact = await SharedCliArtifacts.GetAvaloniaAsync();
        
        // Install first time
        var firstInstall = await _runner.InstallAsync(artifact.PackagePath, force: true);
        Assert.True(firstInstall.IsSuccess, $"First install failed: {firstInstall.CombinedOutput}");
        
        // Act - Install again with force
        var result = await _runner.InstallAsync(artifact.PackagePath, force: true);
        
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


