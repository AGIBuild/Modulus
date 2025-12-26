using Modulus.Cli.IntegrationTests.Infrastructure;
using System.Diagnostics;

namespace Modulus.Cli.IntegrationTests.Commands;

/// <summary>
/// Integration tests for 'modulus new' command.
/// </summary>
public class NewCommandTests : IDisposable
{
    private readonly CliTestContext _context;
    private readonly CliRunner _runner;
    
    public NewCommandTests()
    {
        _context = new CliTestContext();
        _runner = new CliRunner(_context);
    }
    
    [Fact]
    public async Task New_Avalonia_CreatesModuleSuccessfully()
    {
        // Act
        var result = await _runner.NewAsync("TestModule");
        
        // Assert
        Assert.True(result.IsSuccess, $"Command failed: {result.CombinedOutput}");
        Assert.Contains("Created TestModule.sln", result.StandardOutput);
        Assert.Contains("Created TestModule.Core/", result.StandardOutput);
        Assert.Contains("Created TestModule.UI.Avalonia/", result.StandardOutput);
        
        // Verify files exist
        var moduleDir = Path.Combine(_context.WorkingDirectory, "TestModule");
        Assert.True(Directory.Exists(moduleDir), "Module directory not created");
        Assert.True(File.Exists(Path.Combine(moduleDir, "TestModule.sln")), "Solution file not created");
        Assert.True(File.Exists(Path.Combine(moduleDir, "extension.vsixmanifest")), "Manifest not created");
        Assert.True(Directory.Exists(Path.Combine(moduleDir, "TestModule.Core")), "Core project not created");
        Assert.True(Directory.Exists(Path.Combine(moduleDir, "TestModule.UI.Avalonia")), "UI project not created");
    }
    
    [Fact]
    public async Task New_Blazor_CreatesModuleSuccessfully()
    {
        // Act
        var result = await _runner.NewAsync("BlazorModule", template: "module-blazor");
        
        // Assert
        Assert.True(result.IsSuccess, $"Command failed: {result.CombinedOutput}");
        Assert.Contains("Created BlazorModule.sln", result.StandardOutput);
        Assert.Contains("Created BlazorModule.Core/", result.StandardOutput);
        Assert.Contains("Created BlazorModule.UI.Blazor/", result.StandardOutput);
        
        // Verify files exist
        var moduleDir = Path.Combine(_context.WorkingDirectory, "BlazorModule");
        Assert.True(Directory.Exists(moduleDir), "Module directory not created");
        Assert.True(File.Exists(Path.Combine(moduleDir, "BlazorModule.sln")), "Solution file not created");
        Assert.True(Directory.Exists(Path.Combine(moduleDir, "BlazorModule.UI.Blazor")), "UI project not created");
    }

    [Fact]
    public async Task New_AvaloniaApp_CreatesHostAppAndBuildsSuccessfully()
    {
        // Act
        var result = await _runner.NewAsync("TestHostApp", template: "avaloniaapp");

        // Assert (creation)
        Assert.True(result.IsSuccess, $"Command failed: {result.CombinedOutput}");
        Assert.Contains("Created TestHostApp.sln", result.StandardOutput);
        Assert.Contains("Created TestHostApp.Host.Avalonia/", result.StandardOutput);

        var appDir = Path.Combine(_context.WorkingDirectory, "TestHostApp");
        var slnPath = Path.Combine(appDir, "TestHostApp.sln");
        Assert.True(File.Exists(slnPath), "Solution file not created");

        // Assert (build)
        var build = await RunDotNetAsync($"build \"{slnPath}\" -c Release", appDir);
        Assert.True(build.ExitCode == 0, $"dotnet build failed: {build.Output}");
    }

    [Fact]
    public async Task New_BlazorApp_CreatesHostAppAndBuildsSuccessfully()
    {
        // Act
        var result = await _runner.NewAsync("TestBlazorHost", template: "blazorapp");

        // Assert (creation)
        Assert.True(result.IsSuccess, $"Command failed: {result.CombinedOutput}");
        Assert.Contains("Created TestBlazorHost.sln", result.StandardOutput);
        Assert.Contains("Created TestBlazorHost.Host.Blazor/", result.StandardOutput);

        var appDir = Path.Combine(_context.WorkingDirectory, "TestBlazorHost");
        var slnPath = Path.Combine(appDir, "TestBlazorHost.sln");
        Assert.True(File.Exists(slnPath), "Solution file not created");

        // Assert (build)
        var build = await RunDotNetAsync($"build \"{slnPath}\" -c Release", appDir);
        Assert.True(build.ExitCode == 0, $"dotnet build failed: {build.Output}");
    }
    
    [Fact]
    public async Task New_WithOutputOption_CreatesInSpecifiedDirectory()
    {
        // Arrange
        var outputDir = _context.CreateSubDirectory("custom-output");
        
        // Act
        var result = await _runner.NewAsync("CustomModule", outputPath: outputDir);
        
        // Assert
        Assert.True(result.IsSuccess, $"Command failed: {result.CombinedOutput}");
        
        // Verify module created in custom output directory
        var moduleDir = Path.Combine(outputDir, "CustomModule");
        Assert.True(Directory.Exists(moduleDir), "Module not created in custom directory");
        Assert.True(File.Exists(Path.Combine(moduleDir, "CustomModule.sln")), "Solution file not created");
    }
    
    [Fact]
    public async Task New_ForceOverwrite_OverwritesExistingDirectory()
    {
        // Arrange - Create existing directory with content
        var moduleDir = _context.CreateSubDirectory("ExistingModule");
        _context.CreateFile("ExistingModule/old-file.txt", "old content");
        
        // Act
        var result = await _runner.NewAsync("ExistingModule", force: true);
        
        // Assert
        Assert.True(result.IsSuccess, $"Command failed: {result.CombinedOutput}");
        
        // Verify new module created and old file removed
        Assert.True(File.Exists(Path.Combine(moduleDir, "ExistingModule.sln")), "Solution file not created");
        Assert.False(File.Exists(Path.Combine(moduleDir, "old-file.txt")), "Old file should be removed");
    }
    
    [Fact]
    public async Task New_InvalidName_Fails()
    {
        // Act - lowercase name is invalid
        var result = await _runner.NewAsync("invalidname");
        
        // Assert
        Assert.False(result.IsSuccess, "Command should fail for invalid module name");
        Assert.Contains("PascalCase", result.CombinedOutput);
    }
    
    [Fact]
    public async Task New_InvalidTemplate_Fails()
    {
        // Act
        var result = await _runner.NewAsync("TestModule", template: "invalid-template");
        
        // Assert
        Assert.False(result.IsSuccess, "Command should fail for invalid template");
        Assert.Contains("module-avalonia", result.CombinedOutput);
    }
    
    public void Dispose()
    {
        _context.Dispose();
    }

    private static async Task<(int ExitCode, string Output)> RunDotNetAsync(string arguments, string workingDirectory)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = arguments,
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = psi };
        process.Start();

        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();
        await Task.WhenAll(process.WaitForExitAsync(), stdoutTask, stderrTask);

        var output = (await stdoutTask) + Environment.NewLine + (await stderrTask);
        return (process.ExitCode, output);
    }
}


