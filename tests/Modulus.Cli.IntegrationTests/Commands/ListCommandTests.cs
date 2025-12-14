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
        // Arrange - Install a module
        var newResult = await _runner.NewAsync("ListShow", "avalonia");
        Assert.True(newResult.IsSuccess, $"Failed to create module: {newResult.CombinedOutput}");
        
        var moduleDir = Path.Combine(_context.WorkingDirectory, "ListShow");
        var packResult = await _runner.PackAsync(path: moduleDir, output: _context.OutputDirectory);
        Assert.True(packResult.IsSuccess, $"Pack failed: {packResult.CombinedOutput}");
        
        var packages = Directory.GetFiles(_context.OutputDirectory, "*.modpkg");
        var installResult = await _runner.InstallAsync(packages[0], force: true);
        Assert.True(installResult.IsSuccess, $"Install failed: {installResult.CombinedOutput}");
        
        // Act
        var result = await _runner.ListAsync();
        
        // Assert
        Assert.True(result.IsSuccess, $"List failed: {result.CombinedOutput}");
        Assert.Contains("ListShow", result.StandardOutput);
        Assert.Contains("Installed modules", result.StandardOutput);
    }
    
    [Fact]
    public async Task List_Verbose_ShowsDetails()
    {
        // Arrange - Install a module
        var newResult = await _runner.NewAsync("VerboseList", "avalonia");
        Assert.True(newResult.IsSuccess, $"Failed to create module: {newResult.CombinedOutput}");
        
        var moduleDir = Path.Combine(_context.WorkingDirectory, "VerboseList");
        var packResult = await _runner.PackAsync(path: moduleDir, output: _context.OutputDirectory);
        Assert.True(packResult.IsSuccess, $"Pack failed: {packResult.CombinedOutput}");
        
        var packages = Directory.GetFiles(_context.OutputDirectory, "*.modpkg");
        var installResult = await _runner.InstallAsync(packages[0], force: true);
        Assert.True(installResult.IsSuccess, $"Install failed: {installResult.CombinedOutput}");
        
        // Act
        var result = await _runner.ListAsync(verbose: true);
        
        // Assert
        Assert.True(result.IsSuccess, $"List failed: {result.CombinedOutput}");
        Assert.Contains("VerboseList", result.StandardOutput);
        // Verbose should include path
        Assert.Contains("Path:", result.StandardOutput);
    }
    
    [Fact]
    public async Task List_MultipleModules_ShowsAll()
    {
        // Arrange - Install multiple modules
        var modules = new[] { "ModuleA", "ModuleB" };
        
        foreach (var moduleName in modules)
        {
            var newResult = await _runner.NewAsync(moduleName, "avalonia");
            Assert.True(newResult.IsSuccess, $"Failed to create {moduleName}: {newResult.CombinedOutput}");
            
            var moduleDir = Path.Combine(_context.WorkingDirectory, moduleName);
            var outputDir = _context.CreateSubDirectory($"output-{moduleName}");
            var packResult = await _runner.PackAsync(path: moduleDir, output: outputDir);
            Assert.True(packResult.IsSuccess, $"Pack failed for {moduleName}: {packResult.CombinedOutput}");
            
            var packages = Directory.GetFiles(outputDir, "*.modpkg");
            Assert.NotEmpty(packages);
            
            var installResult = await _runner.InstallAsync(packages[0], force: true);
            Assert.True(installResult.IsSuccess, $"Install failed for {moduleName}: {installResult.CombinedOutput}");
        }
        
        // Act
        var result = await _runner.ListAsync();
        
        // Assert
        Assert.True(result.IsSuccess, $"List failed: {result.CombinedOutput}");
        foreach (var moduleName in modules)
        {
            Assert.Contains(moduleName, result.StandardOutput);
        }
        Assert.Contains("Installed modules", result.StandardOutput);
    }
    
    public void Dispose()
    {
        _context.Dispose();
    }
}


