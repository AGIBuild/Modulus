using System.IO.Compression;
using Modulus.Cli.IntegrationTests.Infrastructure;
using Modulus.Core.Manifest;
using Modulus.Sdk;

namespace Modulus.Cli.IntegrationTests.EndToEnd;

/// <summary>
/// Tests that verify generated modules have valid structure and content.
/// </summary>
public class ModuleLoadTests : IDisposable
{
    private readonly CliTestContext _context;
    private readonly CliRunner _runner;
    
    public ModuleLoadTests()
    {
        _context = new CliTestContext();
        _runner = new CliRunner(_context);
    }
    
    [Fact]
    public async Task GeneratedAvaloniaModule_HasValidStructure()
    {
        // Arrange - Create and pack a module
        var moduleName = "LoadableAvalonia";
        var newResult = await _runner.NewAsync(moduleName, "avalonia");
        Assert.True(newResult.IsSuccess, $"[new] Failed: {newResult.CombinedOutput}");
        
        var moduleDir = Path.Combine(_context.WorkingDirectory, moduleName);
        var outputDir = _context.CreateSubDirectory("pack-avalonia");
        var packResult = await _runner.PackAsync(path: moduleDir, output: outputDir);
        Assert.True(packResult.IsSuccess, $"[pack] Failed: {packResult.CombinedOutput}");
        
        // Extract package to simulate installed module
        var packages = Directory.GetFiles(outputDir, "*.modpkg");
        Assert.True(packages.Length > 0, $"No .modpkg found. Pack output: {packResult.CombinedOutput}");
        
        var extractDir = _context.CreateSubDirectory("extracted-avalonia");
        ZipFile.ExtractToDirectory(packages[0], extractDir);
        
        // Assert - Verify module structure is valid for loading
        Assert.True(File.Exists(Path.Combine(extractDir, "extension.vsixmanifest")), "Manifest missing");
        Assert.True(File.Exists(Path.Combine(extractDir, $"{moduleName}.Core.dll")), "Core DLL missing");
        Assert.True(File.Exists(Path.Combine(extractDir, $"{moduleName}.UI.Avalonia.dll")), "UI DLL missing");
    }
    
    [Fact]
    public async Task GeneratedBlazorModule_HasValidStructure()
    {
        // Arrange
        var moduleName = "LoadableBlazor";
        var newResult = await _runner.NewAsync(moduleName, "blazor");
        Assert.True(newResult.IsSuccess, $"[new] Failed: {newResult.CombinedOutput}");
        
        var moduleDir = Path.Combine(_context.WorkingDirectory, moduleName);
        var outputDir = _context.CreateSubDirectory("pack-blazor");
        var packResult = await _runner.PackAsync(path: moduleDir, output: outputDir);
        Assert.True(packResult.IsSuccess, $"[pack] Failed: {packResult.CombinedOutput}");
        
        // Extract package
        var packages = Directory.GetFiles(outputDir, "*.modpkg");
        Assert.True(packages.Length > 0, $"No .modpkg found. Pack output: {packResult.CombinedOutput}");
        
        var extractDir = _context.CreateSubDirectory("extracted-blazor");
        ZipFile.ExtractToDirectory(packages[0], extractDir);
        
        // Assert - Verify module structure is valid for loading
        Assert.True(File.Exists(Path.Combine(extractDir, "extension.vsixmanifest")), "Manifest missing");
        Assert.True(File.Exists(Path.Combine(extractDir, $"{moduleName}.Core.dll")), "Core DLL missing");
        Assert.True(File.Exists(Path.Combine(extractDir, $"{moduleName}.UI.Blazor.dll")), "UI DLL missing");
    }
    
    [Fact]
    public async Task GeneratedModule_HasValidManifest()
    {
        // Arrange
        var moduleName = "ManifestCheck";
        var newResult = await _runner.NewAsync(moduleName, "avalonia");
        Assert.True(newResult.IsSuccess, $"[new] Failed: {newResult.CombinedOutput}");
        
        var moduleDir = Path.Combine(_context.WorkingDirectory, moduleName);
        var outputDir = _context.CreateSubDirectory("pack-manifest");
        var packResult = await _runner.PackAsync(path: moduleDir, output: outputDir);
        Assert.True(packResult.IsSuccess, $"[pack] Failed: {packResult.CombinedOutput}");
        
        // Extract package
        var packages = Directory.GetFiles(outputDir, "*.modpkg");
        Assert.NotEmpty(packages);
        
        var extractDir = _context.CreateSubDirectory("extracted-manifest");
        ZipFile.ExtractToDirectory(packages[0], extractDir);
        
        // Act - Read and validate manifest
        var manifestPath = Path.Combine(extractDir, "extension.vsixmanifest");
        Assert.True(File.Exists(manifestPath), "Manifest not found in package");
        
        var manifest = await VsixManifestReader.ReadFromFileAsync(manifestPath);
        
        // Assert
        Assert.NotNull(manifest);
        Assert.NotNull(manifest.Metadata);
        Assert.NotNull(manifest.Metadata.Identity);
        Assert.Equal(moduleName, manifest.Metadata.DisplayName);
        Assert.False(string.IsNullOrEmpty(manifest.Metadata.Identity.Id));
        Assert.Equal("1.0.0", manifest.Metadata.Identity.Version);
    }
    
    [Fact]
    public async Task GeneratedModule_ContainsRequiredDlls()
    {
        // Arrange
        var moduleName = "DllCheck";
        var newResult = await _runner.NewAsync(moduleName, "avalonia");
        Assert.True(newResult.IsSuccess, $"[new] Failed: {newResult.CombinedOutput}");
        
        var moduleDir = Path.Combine(_context.WorkingDirectory, moduleName);
        var outputDir = _context.CreateSubDirectory("pack-dlls");
        var packResult = await _runner.PackAsync(path: moduleDir, output: outputDir);
        Assert.True(packResult.IsSuccess, $"[pack] Failed: {packResult.CombinedOutput}");
        
        // Extract package
        var packages = Directory.GetFiles(outputDir, "*.modpkg");
        Assert.NotEmpty(packages);
        
        var extractDir = _context.CreateSubDirectory("extracted-dlls");
        ZipFile.ExtractToDirectory(packages[0], extractDir);
        
        // Act
        var dlls = Directory.GetFiles(extractDir, "*.dll");
        
        // Assert - Should have Core and UI DLLs
        Assert.NotEmpty(dlls);
        Assert.Contains(dlls, d => Path.GetFileName(d) == $"{moduleName}.Core.dll");
        Assert.Contains(dlls, d => Path.GetFileName(d) == $"{moduleName}.UI.Avalonia.dll");
        
        // Should NOT have shared assemblies
        Assert.DoesNotContain(dlls, d => Path.GetFileName(d).StartsWith("Modulus."));
        Assert.DoesNotContain(dlls, d => Path.GetFileName(d).StartsWith("Agibuild.Modulus."));
    }
    
    public void Dispose()
    {
        _context.Dispose();
    }
}
