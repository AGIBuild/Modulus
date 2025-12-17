using Modulus.Cli.IntegrationTests.Infrastructure;

namespace Modulus.Cli.IntegrationTests.EndToEnd;

/// <summary>
/// End-to-end tests covering the full module lifecycle:
/// new → build → pack → install → list → uninstall → list (empty)
/// </summary>
public class FullLifecycleTests : IDisposable
{
    private readonly CliTestContext _context;
    private readonly CliRunner _runner;
    
    public FullLifecycleTests()
    {
        _context = new CliTestContext();
        _runner = new CliRunner(_context);
    }
    
    [Fact]
    public async Task FullLifecycle_Avalonia_CompletesSuccessfully()
    {
        await RunFullLifecycleTest("AvaloniaLifecycle", template: null);
    }
    
    [Fact]
    public async Task FullLifecycle_Blazor_CompletesSuccessfully()
    {
        await RunFullLifecycleTest("BlazorLifecycle", template: "module-blazor");
    }
    
    private async Task RunFullLifecycleTest(string moduleName, string? template)
    {
        // Step 1: Create new module
        var newResult = await _runner.NewAsync(moduleName, template: template, force: true);
        Assert.True(newResult.IsSuccess, $"[new] Failed: {newResult.CombinedOutput}");
        Assert.Contains($"Created {moduleName}.sln", newResult.StandardOutput);
        
        var moduleDir = Path.Combine(_context.WorkingDirectory, moduleName);
        Assert.True(Directory.Exists(moduleDir), "Module directory not created");
        Assert.True(File.Exists(Path.Combine(moduleDir, "extension.vsixmanifest")), "Manifest not created");
        
        // Step 2: Build module
        var buildResult = await _runner.BuildAsync(path: moduleDir, configuration: "Release");
        Assert.True(buildResult.IsSuccess, $"[build] Failed: {buildResult.CombinedOutput}");
        Assert.Contains("Build succeeded", buildResult.StandardOutput);
        
        // Step 3: Pack module
        // Pack already has access to build output; avoid building twice.
        var packResult = await _runner.PackAsync(path: moduleDir, output: _context.OutputDirectory, noBuild: true);
        Assert.True(packResult.IsSuccess, $"[pack] Failed: {packResult.CombinedOutput}");
        Assert.Contains("Packaging complete", packResult.StandardOutput);
        
        var packages = Directory.GetFiles(_context.OutputDirectory, "*.modpkg");
        Assert.Single(packages);
        var packagePath = packages[0];
        Assert.Contains(moduleName, Path.GetFileName(packagePath));
        
        // Step 4: Install module
        var installResult = await _runner.InstallAsync(packagePath, force: true);
        Assert.True(installResult.IsSuccess, $"[install] Failed: {installResult.CombinedOutput}");
        Assert.Contains("installed successfully", installResult.StandardOutput);
        
        // Step 5: List modules - should show installed module
        var listResult = await _runner.ListAsync();
        Assert.True(listResult.IsSuccess, $"[list] Failed: {listResult.CombinedOutput}");
        Assert.Contains(moduleName, listResult.StandardOutput);
        Assert.Contains("Installed modules", listResult.StandardOutput);
        
        // Step 6: Uninstall module
        var uninstallResult = await _runner.UninstallAsync(moduleName, force: true);
        Assert.True(uninstallResult.IsSuccess, $"[uninstall] Failed: {uninstallResult.CombinedOutput}");
        Assert.Contains("uninstalled successfully", uninstallResult.StandardOutput);
        
        // Step 7: List modules - should be empty
        var listAfterResult = await _runner.ListAsync();
        Assert.True(listAfterResult.IsSuccess, $"[list after] Failed: {listAfterResult.CombinedOutput}");
        Assert.Contains("No modules installed", listAfterResult.StandardOutput);
    }
    
    [Fact]
    public async Task FullLifecycle_ReinstallModule_Succeeds()
    {
        var moduleName = "ReinstallTest";
        
        // Create and pack module
        var newResult = await _runner.NewAsync(moduleName, force: true);
        Assert.True(newResult.IsSuccess, $"[new] Failed: {newResult.CombinedOutput}");
        
        var moduleDir = Path.Combine(_context.WorkingDirectory, moduleName);
        var packResult = await _runner.PackAsync(path: moduleDir, output: _context.OutputDirectory);
        Assert.True(packResult.IsSuccess, $"[pack] Failed: {packResult.CombinedOutput}");
        
        var packages = Directory.GetFiles(_context.OutputDirectory, "*.modpkg");
        var packagePath = packages[0];
        
        // Install first time
        var install1 = await _runner.InstallAsync(packagePath, force: true);
        Assert.True(install1.IsSuccess, $"[install 1] Failed: {install1.CombinedOutput}");
        
        var list1 = await _runner.ListAsync();
        Assert.Contains(moduleName, list1.StandardOutput);
        
        // Uninstall
        var uninstall = await _runner.UninstallAsync(moduleName, force: true);
        Assert.True(uninstall.IsSuccess, $"[uninstall] Failed: {uninstall.CombinedOutput}");
        
        var list2 = await _runner.ListAsync();
        Assert.Contains("no modules installed", list2.StandardOutput.ToLowerInvariant());
        
        // Install again
        var install2 = await _runner.InstallAsync(packagePath, force: true);
        Assert.True(install2.IsSuccess, $"[install 2] Failed: {install2.CombinedOutput}");
        
        var list3 = await _runner.ListAsync();
        Assert.Contains(moduleName, list3.StandardOutput);
        Assert.Contains("Installed modules", list3.StandardOutput);
    }
    
    [Fact]
    public async Task FullLifecycle_UpdateModule_ViaForceInstall()
    {
        var moduleName = "UpdateTest";
        
        // Create, pack and install version 1
        var newResult = await _runner.NewAsync(moduleName, force: true);
        Assert.True(newResult.IsSuccess, $"[new] Failed: {newResult.CombinedOutput}");
        
        var moduleDir = Path.Combine(_context.WorkingDirectory, moduleName);
        var output1 = _context.CreateSubDirectory("output-v1");
        var packResult = await _runner.PackAsync(path: moduleDir, output: output1);
        Assert.True(packResult.IsSuccess, $"[pack v1] Failed: {packResult.CombinedOutput}");
        
        var packages = Directory.GetFiles(output1, "*.modpkg");
        var install1 = await _runner.InstallAsync(packages[0], force: true);
        Assert.True(install1.IsSuccess, $"[install v1] Failed: {install1.CombinedOutput}");
        
        // Update manifest version and rebuild
        var manifestPath = Path.Combine(moduleDir, "extension.vsixmanifest");
        var manifestContent = await File.ReadAllTextAsync(manifestPath);
        manifestContent = manifestContent.Replace("1.0.0", "2.0.0");
        await File.WriteAllTextAsync(manifestPath, manifestContent);
        
        // Pack version 2
        var output2 = _context.CreateSubDirectory("output-v2");
        // Avoid a second build; only manifest version changes for packaging.
        var packResult2 = await _runner.PackAsync(path: moduleDir, output: output2, noBuild: true);
        Assert.True(packResult2.IsSuccess, $"[pack v2] Failed: {packResult2.CombinedOutput}");
        
        // Force install version 2
        var packages2 = Directory.GetFiles(output2, "*.modpkg");
        var install2 = await _runner.InstallAsync(packages2[0], force: true);
        Assert.True(install2.IsSuccess, $"[install v2] Failed: {install2.CombinedOutput}");
        
        // Verify version 2 is installed
        var listResult = await _runner.ListAsync(verbose: true);
        Assert.Contains(moduleName, listResult.StandardOutput);
        Assert.Contains("v2.0.0", listResult.StandardOutput);
    }
    
    public void Dispose()
    {
        _context.Dispose();
    }
}


