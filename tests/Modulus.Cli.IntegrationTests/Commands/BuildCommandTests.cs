using Modulus.Cli.IntegrationTests.Infrastructure;

namespace Modulus.Cli.IntegrationTests.Commands;

/// <summary>
/// Integration tests for 'modulus build' command.
/// </summary>
public class BuildCommandTests : IDisposable
{
    private readonly CliTestContext _context;
    private readonly CliRunner _runner;
    
    public BuildCommandTests()
    {
        _context = new CliTestContext();
        _runner = new CliRunner(_context);
    }
    
    [Fact]
    public async Task Build_ValidModule_Succeeds()
    {
        // Arrange - Create a new module first
        var newResult = await _runner.NewAsync("BuildTest", "avalonia");
        Assert.True(newResult.IsSuccess, $"Failed to create module: {newResult.CombinedOutput}");
        
        var moduleDir = Path.Combine(_context.WorkingDirectory, "BuildTest");
        
        // Act
        var result = await _runner.BuildAsync(path: moduleDir, configuration: "Release");
        
        // Assert
        Assert.True(result.IsSuccess, $"Build failed: {result.CombinedOutput}");
        Assert.Contains("Build succeeded", result.StandardOutput);
    }
    
    [Fact]
    public async Task Build_Debug_Configuration_Succeeds()
    {
        // Arrange
        var newResult = await _runner.NewAsync("DebugBuild", "avalonia");
        Assert.True(newResult.IsSuccess, $"Failed to create module: {newResult.CombinedOutput}");
        
        var moduleDir = Path.Combine(_context.WorkingDirectory, "DebugBuild");
        
        // Act
        var result = await _runner.BuildAsync(path: moduleDir, configuration: "Debug");
        
        // Assert
        Assert.True(result.IsSuccess, $"Build failed: {result.CombinedOutput}");
        Assert.Contains("Build succeeded", result.StandardOutput);
    }
    
    [Fact]
    public async Task Build_NonExistentPath_Fails()
    {
        // Act
        var result = await _runner.BuildAsync(path: "/nonexistent/path");
        
        // Assert
        Assert.False(result.IsSuccess);
        // May show "DirectoryNotFoundException" or similar
        Assert.True(result.CombinedOutput.Contains("not found", StringComparison.OrdinalIgnoreCase) ||
                   result.CombinedOutput.Contains("DirectoryNotFoundException", StringComparison.OrdinalIgnoreCase) ||
                   result.CombinedOutput.Contains("does not exist", StringComparison.OrdinalIgnoreCase));
    }
    
    [Fact]
    public async Task Build_NoModuleProject_Fails()
    {
        // Arrange - Create empty directory
        var emptyDir = _context.CreateSubDirectory("empty");
        
        // Act
        var result = await _runner.BuildAsync(path: emptyDir);
        
        // Assert
        // The CLI writes "No module project found" to output
        Assert.Contains("No module project found", result.CombinedOutput);
    }
    
    [Fact(Skip = "Verbose build has process wait issues in test environment")]
    public async Task Build_Verbose_ShowsDetails()
    {
        // Arrange
        var newResult = await _runner.NewAsync("VerboseBuild", "avalonia");
        Assert.True(newResult.IsSuccess, $"Failed to create module: {newResult.CombinedOutput}");
        
        var moduleDir = Path.Combine(_context.WorkingDirectory, "VerboseBuild");
        
        // Act
        var result = await _runner.BuildAsync(path: moduleDir, verbose: true);
        
        // Assert
        Assert.True(result.IsSuccess, $"Build failed: {result.CombinedOutput}");
        // Verbose output should contain more details
        Assert.Contains("Build succeeded", result.StandardOutput);
    }
    
    public void Dispose()
    {
        _context.Dispose();
    }
}


