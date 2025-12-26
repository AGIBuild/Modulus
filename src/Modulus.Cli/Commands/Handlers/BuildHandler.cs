using Modulus.Cli.Services;

namespace Modulus.Cli.Commands.Handlers;

/// <summary>
/// Handles module build logic.
/// </summary>
public sealed class BuildHandler
{
    private readonly ICliConsole _console;
    private readonly IProcessRunner _processRunner;

    public BuildHandler(ICliConsole console, IProcessRunner processRunner)
    {
        _console = console;
        _processRunner = processRunner;
    }

    public async Task<int> ExecuteAsync(string? path, string configuration, bool verbose, CancellationToken cancellationToken)
    {
        var projectDir = string.IsNullOrWhiteSpace(path)
            ? Directory.GetCurrentDirectory()
            : Path.GetFullPath(path);

        if (!Directory.Exists(projectDir))
        {
            await _console.Error.WriteLineAsync($"Error: Directory not found: {projectDir}");
            return 1;
        }

        var (projectFile, _) = ProjectLocator.FindProjectFile(projectDir);
        if (projectFile == null)
        {
            await _console.Error.WriteLineAsync("Error: No module project found in the current directory.");
            await _console.Error.WriteLineAsync("Expected: .sln file or .csproj file with extension.vsixmanifest");
            return 1;
        }

        await _console.Out.WriteLineAsync($"Building module: {Path.GetFileName(projectDir)}");
        await _console.Out.WriteLineAsync($"  Project: {Path.GetFileName(projectFile)}");
        await _console.Out.WriteLineAsync($"  Configuration: {configuration}");
        await _console.Out.WriteLineAsync();

        var result = await _processRunner.RunAsync(
            new ProcessRunRequest(
                "dotnet",
                $"build \"{projectFile}\" --configuration {configuration}",
                projectDir,
                RedirectOutput: verbose),
            cancellationToken);

        if (verbose)
        {
            if (!string.IsNullOrWhiteSpace(result.StandardOutput))
            {
                await _console.Out.WriteLineAsync(result.StandardOutput);
            }
            if (!string.IsNullOrWhiteSpace(result.StandardError))
            {
                await _console.Error.WriteLineAsync(result.StandardError);
            }
        }

        if (result.ExitCode == 0)
        {
            await _console.Out.WriteLineAsync();
            await _console.Out.WriteLineAsync("✓ Build succeeded!");

            var outputDir = ProjectLocator.FindOutputDirectory(projectDir, configuration);
            if (outputDir != null)
            {
                await _console.Out.WriteLineAsync($"  Output: {outputDir}");
            }

            return 0;
        }

        await _console.Out.WriteLineAsync();
        await _console.Out.WriteLineAsync("✗ Build failed!");
        if (!verbose)
        {
            await _console.Out.WriteLineAsync("  Run with --verbose for detailed output.");
        }

        return 1;
    }
}


