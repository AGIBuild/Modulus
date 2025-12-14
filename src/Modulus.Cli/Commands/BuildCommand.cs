using System.CommandLine;
using System.Diagnostics;

namespace Modulus.Cli.Commands;

/// <summary>
/// Build command: modulus build
/// Builds the module project in the current directory.
/// </summary>
public static class BuildCommand
{
    public static Command Create()
    {
        var pathOption = new Option<string?>("--path", "-p") { Description = "Path to module project directory (default: current directory)" };
        var configurationOption = new Option<string?>("--configuration", "-c") { Description = "Build configuration (Debug/Release, default: Release)" };
        var verboseOption = new Option<bool>("--verbose", "-v") { Description = "Show detailed build output" };

        var command = new Command("build", "Build the module project in the current directory");
        command.Options.Add(pathOption);
        command.Options.Add(configurationOption);
        command.Options.Add(verboseOption);

        command.SetAction(async (parseResult, cancellationToken) =>
        {
            var path = parseResult.GetValue(pathOption);
            var configuration = parseResult.GetValue(configurationOption) ?? "Release";
            var verbose = parseResult.GetValue(verboseOption);
            await HandleAsync(path, configuration, verbose);
        });

        return command;
    }

    private static async Task HandleAsync(string? path, string configuration, bool verbose)
    {
        var projectDir = path ?? Directory.GetCurrentDirectory();

        // Find solution or project file
        var (projectFile, projectType) = FindProjectFile(projectDir);
        if (projectFile == null)
        {
            Console.WriteLine("Error: No module project found in the current directory.");
            Console.WriteLine("Expected: .sln file or .csproj file with extension.vsixmanifest");
            return;
        }

        Console.WriteLine($"Building module: {Path.GetFileName(projectDir)}");
        Console.WriteLine($"  Project: {Path.GetFileName(projectFile)}");
        Console.WriteLine($"  Configuration: {configuration}");
        Console.WriteLine();

        // Run dotnet build
        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"build \"{projectFile}\" --configuration {configuration}",
            WorkingDirectory = projectDir,
            RedirectStandardOutput = !verbose,
            RedirectStandardError = !verbose,
            UseShellExecute = false
        };

        using var process = Process.Start(psi);
        if (process == null)
        {
            Console.WriteLine("Error: Failed to start dotnet build process");
            return;
        }

        if (!verbose)
        {
            // Show progress indicator
            Console.Write("Building");
            while (!process.HasExited)
            {
                Console.Write(".");
                await Task.Delay(500);
            }
            Console.WriteLine();
        }

        await process.WaitForExitAsync();

        if (process.ExitCode == 0)
        {
            Console.WriteLine();
            Console.WriteLine("✓ Build succeeded!");
            
            // Find output directory
            var outputDir = FindOutputDirectory(projectDir, configuration);
            if (outputDir != null)
            {
                Console.WriteLine($"  Output: {outputDir}");
            }
        }
        else
        {
            Console.WriteLine();
            Console.WriteLine("✗ Build failed!");
            
            if (!verbose)
            {
                Console.WriteLine("  Run with --verbose for detailed output.");
            }
        }
    }

    /// <summary>
    /// Find the project file to build (.sln or .csproj)
    /// </summary>
    internal static (string? Path, string? Type) FindProjectFile(string directory)
    {
        // Prefer solution file
        var slnFiles = Directory.GetFiles(directory, "*.sln");
        if (slnFiles.Length > 0)
        {
            return (slnFiles[0], "solution");
        }

        // Look for .csproj files
        var csprojFiles = Directory.GetFiles(directory, "*.csproj");
        if (csprojFiles.Length > 0)
        {
            // Check if this looks like a module project (has manifest or references Modulus.Sdk)
            foreach (var csproj in csprojFiles)
            {
                var content = File.ReadAllText(csproj);
                if (content.Contains("Modulus.Sdk") || content.Contains("Agibuild.Modulus.Sdk"))
                {
                    return (csproj, "project");
                }
            }
            
            // Fallback to first csproj
            return (csprojFiles[0], "project");
        }

        // Check subdirectories for Core project
        foreach (var subDir in Directory.GetDirectories(directory))
        {
            var subCsproj = Directory.GetFiles(subDir, "*.csproj");
            if (subCsproj.Length > 0)
            {
                // Found a subproject, use the parent as solution-like build
                var parentSln = Directory.GetFiles(directory, "*.sln");
                if (parentSln.Length > 0)
                {
                    return (parentSln[0], "solution");
                }
            }
        }

        return (null, null);
    }

    /// <summary>
    /// Find the build output directory
    /// </summary>
    internal static string? FindOutputDirectory(string projectDir, string configuration)
    {
        // Check common output paths
        var candidates = new[]
        {
            Path.Combine(projectDir, "bin", configuration),
            Path.Combine(projectDir, "bin", configuration, "net9.0"),
            Path.Combine(projectDir, "bin", configuration, "net8.0"),
            Path.Combine(projectDir, "output"),
        };

        foreach (var candidate in candidates)
        {
            if (Directory.Exists(candidate))
            {
                return candidate;
            }
        }

        // Search for Core.dll in bin folder
        var binDir = Path.Combine(projectDir, "bin");
        if (Directory.Exists(binDir))
        {
            var dllFiles = Directory.GetFiles(binDir, "*.Core.dll", SearchOption.AllDirectories);
            if (dllFiles.Length > 0)
            {
                return Path.GetDirectoryName(dllFiles[0]);
            }
        }

        return null;
    }
}
