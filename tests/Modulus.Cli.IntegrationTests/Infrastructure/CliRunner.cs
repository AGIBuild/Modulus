using System.Diagnostics;
using Modulus.Cli.Commands.Handlers;
using Modulus.Cli.Services;

namespace Modulus.Cli.IntegrationTests.Infrastructure;

/// <summary>
/// Executes CLI commands for integration testing.
/// - Commands without database (new/build/pack): Process execution
/// - Commands with database (install/uninstall/list): Direct handler invocation with injected ServiceProvider
/// </summary>
public class CliRunner
{
    private readonly CliTestContext _context;
    private readonly string _cliPath;
    
    /// <summary>
    /// Default timeout for CLI commands.
    /// </summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(5);
    
    public CliRunner(CliTestContext context)
    {
        _context = context;
        _cliPath = FindCliExecutable();
    }
    
    /// <summary>
    /// Runs a CLI command via process execution.
    /// Used for commands that don't require database access.
    /// </summary>
    public async Task<CliResult> RunProcessAsync(string arguments, string? workingDirectory = null)
    {
        var effectiveWorkingDir = workingDirectory ?? _context.WorkingDirectory;
        
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"\"{_cliPath}\" {arguments}",
                WorkingDirectory = effectiveWorkingDir,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            
            using var process = new Process { StartInfo = psi };
            
            var outputBuilder = new System.Text.StringBuilder();
            var errorBuilder = new System.Text.StringBuilder();
            
            process.OutputDataReceived += (_, e) =>
            {
                if (e.Data != null) outputBuilder.AppendLine(e.Data);
            };
            process.ErrorDataReceived += (_, e) =>
            {
                if (e.Data != null) errorBuilder.AppendLine(e.Data);
            };
            
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            
            using var cts = new CancellationTokenSource(Timeout);
            try
            {
                await process.WaitForExitAsync(cts.Token);
            }
            catch (OperationCanceledException)
            {
                process.Kill(entireProcessTree: true);
                return new CliResult
                {
                    ExitCode = -1,
                    StandardOutput = outputBuilder.ToString(),
                    StandardError = $"Command timed out after {Timeout.TotalSeconds}s\n{errorBuilder}",
                    Exception = new TimeoutException($"CLI command timed out: {arguments}")
                };
            }
            
            return new CliResult
            {
                ExitCode = process.ExitCode,
                StandardOutput = outputBuilder.ToString(),
                StandardError = errorBuilder.ToString()
            };
        }
        catch (Exception ex)
        {
            return new CliResult
            {
                ExitCode = -1,
                StandardError = ex.Message,
                Exception = ex
            };
        }
    }
    
    /// <summary>
    /// Runs 'modulus new' command to create a new module.
    /// </summary>
    public Task<CliResult> NewAsync(
        string moduleName, 
        string target, 
        string? outputPath = null,
        bool force = false)
    {
        var args = $"new {moduleName} --target {target}";
        if (!string.IsNullOrEmpty(outputPath))
        {
            args += $" --output \"{outputPath}\"";
        }
        if (force)
        {
            args += " --force";
        }
        return RunProcessAsync(args);
    }
    
    /// <summary>
    /// Runs 'modulus build' command.
    /// </summary>
    public Task<CliResult> BuildAsync(
        string? path = null, 
        string configuration = "Release",
        bool verbose = false)
    {
        var args = "build";
        if (!string.IsNullOrEmpty(path))
        {
            args += $" --path \"{path}\"";
        }
        args += $" --configuration {configuration}";
        if (verbose)
        {
            args += " --verbose";
        }
        return RunProcessAsync(args);
    }
    
    /// <summary>
    /// Runs 'modulus pack' command.
    /// </summary>
    public Task<CliResult> PackAsync(
        string? path = null,
        string? output = null,
        string configuration = "Release",
        bool noBuild = false,
        bool verbose = false)
    {
        var args = "pack";
        if (!string.IsNullOrEmpty(path))
        {
            args += $" --path \"{path}\"";
        }
        if (!string.IsNullOrEmpty(output))
        {
            args += $" --output \"{output}\"";
        }
        args += $" --configuration {configuration}";
        if (noBuild)
        {
            args += " --no-build";
        }
        if (verbose)
        {
            args += " --verbose";
        }
        return RunProcessAsync(args);
    }
    
    /// <summary>
    /// Runs 'modulus install' command using direct handler invocation.
    /// This ensures test database isolation.
    /// </summary>
    public async Task<CliResult> InstallAsync(string source, bool force = false, bool verbose = false)
    {
        try
        {
            var provider = await _context.GetServiceProviderAsync();
            var handler = new InstallHandler(provider, _context.ModulesDirectory);
            
            using var outputWriter = new StringWriter();
            var result = await handler.ExecuteAsync(source, force, outputWriter);
            
            return new CliResult
            {
                ExitCode = result.Success ? 0 : 1,
                StandardOutput = outputWriter.ToString(),
                StandardError = result.Success ? "" : result.Message
            };
        }
        catch (Exception ex)
        {
            return new CliResult
            {
                ExitCode = 1,
                StandardError = ex.Message,
                Exception = ex
            };
        }
    }
    
    /// <summary>
    /// Runs 'modulus uninstall' command using direct handler invocation.
    /// </summary>
    public async Task<CliResult> UninstallAsync(string module, bool force = false, bool verbose = false)
    {
        try
        {
            var provider = await _context.GetServiceProviderAsync();
            var handler = new UninstallHandler(provider, _context.ModulesDirectory);
            
            using var outputWriter = new StringWriter();
            var result = await handler.ExecuteAsync(module, force, outputWriter);
            
            return new CliResult
            {
                ExitCode = result.Success ? 0 : 1,
                StandardOutput = outputWriter.ToString(),
                StandardError = result.Success ? "" : result.Message
            };
        }
        catch (Exception ex)
        {
            return new CliResult
            {
                ExitCode = 1,
                StandardError = ex.Message,
                Exception = ex
            };
        }
    }
    
    /// <summary>
    /// Runs 'modulus list' command using direct handler invocation.
    /// </summary>
    public async Task<CliResult> ListAsync(bool verbose = false)
    {
        try
        {
            var provider = await _context.GetServiceProviderAsync();
            var handler = new ListHandler(provider);
            
            using var outputWriter = new StringWriter();
            var result = await handler.ExecuteAsync(verbose, outputWriter);
            
            return new CliResult
            {
                ExitCode = result.Success ? 0 : 1,
                StandardOutput = outputWriter.ToString(),
                StandardError = result.Success ? "" : result.Message
            };
        }
        catch (Exception ex)
        {
            return new CliResult
            {
                ExitCode = 1,
                StandardError = ex.Message,
                Exception = ex
            };
        }
    }
    
    /// <summary>
    /// Finds the CLI executable path.
    /// </summary>
    private static string FindCliExecutable()
    {
        // Look for the CLI in artifacts/cli directory
        var solutionRoot = FindSolutionRoot();
        var cliPath = Path.Combine(solutionRoot, "artifacts", "cli", "modulus.dll");
        
        if (File.Exists(cliPath))
        {
            return cliPath;
        }
        
        // Fallback: try to find it relative to test assembly
        var assemblyDir = Path.GetDirectoryName(typeof(CliRunner).Assembly.Location) ?? ".";
        var fallbackPath = Path.Combine(assemblyDir, "..", "..", "..", "..", "cli", "modulus.dll");
        if (File.Exists(fallbackPath))
        {
            return Path.GetFullPath(fallbackPath);
        }
        
        throw new FileNotFoundException(
            $"CLI executable not found. Expected at: {cliPath}. " +
            "Run 'nuke compile' first to build the CLI.");
    }
    
    /// <summary>
    /// Finds the solution root directory.
    /// </summary>
    private static string FindSolutionRoot()
    {
        var current = Directory.GetCurrentDirectory();
        while (!string.IsNullOrEmpty(current))
        {
            if (File.Exists(Path.Combine(current, "Modulus.sln")))
            {
                return current;
            }
            current = Path.GetDirectoryName(current);
        }
        
        // Try from assembly location
        current = Path.GetDirectoryName(typeof(CliRunner).Assembly.Location);
        while (!string.IsNullOrEmpty(current))
        {
            if (File.Exists(Path.Combine(current, "Modulus.sln")))
            {
                return current;
            }
            current = Path.GetDirectoryName(current);
        }
        
        throw new DirectoryNotFoundException("Could not find Modulus.sln in parent directories");
    }
}
