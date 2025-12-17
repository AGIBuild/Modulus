using Modulus.Cli.IntegrationTests.Infrastructure;

namespace Modulus.Cli.IntegrationTests.Commands;

/// <summary>
/// Integration tests for 'modulus list' command.
/// </summary>
public class ListCommandTests : IDisposable
{
    private readonly CliTestContext _context;
    private readonly CliRunner _runner;
    
    public ListCommandTests()
    {
        _context = new CliTestContext();
        _runner = new CliRunner(_context);
    }
    
    [Fact]
    public async Task List_EmptyDatabase_ShowsNoModules()
    {
        // Act
        var result = await _runner.ListAsync();
        
        // Assert
        Assert.True(result.IsSuccess, $"List failed: {result.CombinedOutput}");
        Assert.Contains("No modules installed", result.StandardOutput);
    }
    
    [Fact]
    public async Task List_WithInstalledModule_ShowsModule()
    {
        // Arrange - reuse shared prebuilt package
        var artifact = await SharedCliArtifacts.GetAvaloniaAsync();
        var installResult = await _runner.InstallAsync(artifact.PackagePath, force: true);
        Assert.True(installResult.IsSuccess, $"Install failed: {installResult.CombinedOutput}");
        
        // Act
        var result = await _runner.ListAsync();
        
        // Assert
        Assert.True(result.IsSuccess, $"List failed: {result.CombinedOutput}");
        Assert.Contains(artifact.ModuleName, result.StandardOutput);
        Assert.Contains("Installed modules", result.StandardOutput);
    }
    
    [Fact]
    public async Task List_Verbose_ShowsDetails()
    {
        // Arrange - reuse shared prebuilt package
        var artifact = await SharedCliArtifacts.GetAvaloniaAsync();
        var installResult = await _runner.InstallAsync(artifact.PackagePath, force: true);
        Assert.True(installResult.IsSuccess, $"Install failed: {installResult.CombinedOutput}");
        
        // Act
        var result = await _runner.ListAsync(verbose: true);
        
        // Assert
        Assert.True(result.IsSuccess, $"List failed: {result.CombinedOutput}");
        Assert.Contains(artifact.ModuleName, result.StandardOutput);
        // Verbose should include path
        Assert.Contains("Path:", result.StandardOutput);
    }
    
    [Fact]
    public async Task List_MultipleModules_ShowsAll()
    {
        // Arrange - install two shared prebuilt modules (distinct identities)
        var a = await SharedCliArtifacts.GetAvaloniaAAsync();
        var b = await SharedCliArtifacts.GetAvaloniaBAsync();

        var installA = await _runner.InstallAsync(a.PackagePath, force: true);
        Assert.True(installA.IsSuccess, $"Install failed for {a.ModuleName}: {installA.CombinedOutput}");

        var installB = await _runner.InstallAsync(b.PackagePath, force: true);
        Assert.True(installB.IsSuccess, $"Install failed for {b.ModuleName}: {installB.CombinedOutput}");
        
        // Act
        var result = await _runner.ListAsync();
        
        // Assert
        Assert.True(result.IsSuccess, $"List failed: {result.CombinedOutput}");
        Assert.Contains(a.ModuleName, result.StandardOutput);
        Assert.Contains(b.ModuleName, result.StandardOutput);
        Assert.Contains("Installed modules", result.StandardOutput);
    }
    
    public void Dispose()
    {
        _context.Dispose();
    }
}


