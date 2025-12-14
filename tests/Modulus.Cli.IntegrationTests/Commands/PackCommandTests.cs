using System.IO.Compression;
using Modulus.Cli.IntegrationTests.Infrastructure;

namespace Modulus.Cli.IntegrationTests.Commands;

/// <summary>
/// Integration tests for 'modulus pack' command.
/// </summary>
public class PackCommandTests : IDisposable
{
    private readonly CliTestContext _context;
    private readonly CliRunner _runner;
    
    public PackCommandTests()
    {
        _context = new CliTestContext();
        _runner = new CliRunner(_context);
    }
    
    [Fact]
    public async Task Pack_ValidModule_CreatesPackage()
    {
        // Arrange - Create and build a module
        var newResult = await _runner.NewAsync("PackTest", "avalonia");
        Assert.True(newResult.IsSuccess, $"Failed to create module: {newResult.CombinedOutput}");
        
        var moduleDir = Path.Combine(_context.WorkingDirectory, "PackTest");
        
        // Act
        var result = await _runner.PackAsync(path: moduleDir, output: _context.OutputDirectory);
        
        // Assert
        Assert.True(result.IsSuccess, $"Pack failed: {result.CombinedOutput}");
        Assert.Contains("Packaging complete", result.StandardOutput);
        
        // Verify package created
        var packages = Directory.GetFiles(_context.OutputDirectory, "*.modpkg");
        Assert.Single(packages);
        Assert.Contains("PackTest", Path.GetFileName(packages[0]));
    }
    
    [Fact]
    public async Task Pack_WithNoBuild_UsesExistingBuild()
    {
        // Arrange - Create, build, then pack with --no-build
        var newResult = await _runner.NewAsync("NoBuildPack", "avalonia");
        Assert.True(newResult.IsSuccess, $"Failed to create module: {newResult.CombinedOutput}");
        
        var moduleDir = Path.Combine(_context.WorkingDirectory, "NoBuildPack");
        
        var buildResult = await _runner.BuildAsync(path: moduleDir);
        Assert.True(buildResult.IsSuccess, $"Build failed: {buildResult.CombinedOutput}");
        
        // Act
        var result = await _runner.PackAsync(path: moduleDir, output: _context.OutputDirectory, noBuild: true);
        
        // Assert
        Assert.True(result.IsSuccess, $"Pack failed: {result.CombinedOutput}");
        Assert.Contains("Skipping build", result.StandardOutput);
        Assert.Contains("Packaging complete", result.StandardOutput);
    }
    
    [Fact]
    public async Task Pack_PackageContentsValid()
    {
        // Arrange
        var newResult = await _runner.NewAsync("ContentCheck", "avalonia");
        Assert.True(newResult.IsSuccess, $"Failed to create module: {newResult.CombinedOutput}");
        
        var moduleDir = Path.Combine(_context.WorkingDirectory, "ContentCheck");
        var packOutputDir = _context.CreateSubDirectory("pack-output");
        
        // Act
        var result = await _runner.PackAsync(path: moduleDir, output: packOutputDir);
        Assert.True(result.IsSuccess, $"Pack failed: {result.CombinedOutput}");
        
        // Assert - Verify package contents
        var packages = Directory.GetFiles(packOutputDir, "*.modpkg");
        Assert.True(packages.Length > 0, $"No .modpkg files found in {packOutputDir}. Output: {result.CombinedOutput}");
        
        var extractDir = _context.CreateSubDirectory("extracted");
        ZipFile.ExtractToDirectory(packages[0], extractDir);
        
        // Must contain manifest
        Assert.True(File.Exists(Path.Combine(extractDir, "extension.vsixmanifest")), 
            "Package missing extension.vsixmanifest");
        
        // Must contain module DLLs
        var dlls = Directory.GetFiles(extractDir, "*.dll");
        Assert.NotEmpty(dlls);
        
        // Verify Core and UI DLLs exist
        Assert.Contains(dlls, d => Path.GetFileName(d).Contains("ContentCheck.Core"));
        Assert.Contains(dlls, d => Path.GetFileName(d).Contains("ContentCheck.UI.Avalonia"));
        
        // Should NOT contain shared assemblies
        Assert.DoesNotContain(dlls, d => Path.GetFileName(d).StartsWith("Modulus.Core"));
        Assert.DoesNotContain(dlls, d => Path.GetFileName(d).StartsWith("Modulus.Sdk"));
    }
    
    [Fact]
    public async Task Pack_BlazorModule_CreatesPackage()
    {
        // Arrange
        var newResult = await _runner.NewAsync("BlazorPack", "blazor");
        Assert.True(newResult.IsSuccess, $"Failed to create module: {newResult.CombinedOutput}");
        
        var moduleDir = Path.Combine(_context.WorkingDirectory, "BlazorPack");
        
        // Act
        var result = await _runner.PackAsync(path: moduleDir, output: _context.OutputDirectory);
        
        // Assert
        Assert.True(result.IsSuccess, $"Pack failed: {result.CombinedOutput}");
        
        var packages = Directory.GetFiles(_context.OutputDirectory, "*.modpkg");
        Assert.Single(packages);
        Assert.Contains("BlazorPack", Path.GetFileName(packages[0]));
    }
    
    [Fact]
    public async Task Pack_NoManifest_Fails()
    {
        // Arrange - Create directory without manifest but with a project
        var emptyDir = _context.CreateSubDirectory("no-manifest");
        _context.CreateFile("no-manifest/test.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\"></Project>");
        
        // Act
        var result = await _runner.PackAsync(path: emptyDir);
        
        // Assert - should fail because no manifest found
        Assert.Contains("manifest", result.CombinedOutput.ToLowerInvariant());
    }
    
    public void Dispose()
    {
        _context.Dispose();
    }
}


