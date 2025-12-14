using System.CommandLine;
using System.CommandLine.Parsing;
using System.Diagnostics;
using System.IO.Compression;
using System.Xml.Linq;

namespace Modulus.Cli.Commands;

/// <summary>
/// Pack command: modulus pack
/// Builds and packages the module into a .modpkg file for distribution.
/// </summary>
public static class PackCommand
{
    /// <summary>
    /// Shared assemblies that should NOT be included in module packages.
    /// These are provided by the host runtime.
    /// </summary>
    private static readonly HashSet<string> SharedAssemblyPrefixes = new(StringComparer.OrdinalIgnoreCase)
    {
        "Modulus.Core",
        "Modulus.Sdk",
        "Modulus.UI.Abstractions",
        "Modulus.UI.Avalonia",
        "Modulus.UI.Blazor",
        "Agibuild.Modulus.Core",
        "Agibuild.Modulus.Sdk",
        "Agibuild.Modulus.UI.Abstractions",
        "Agibuild.Modulus.UI.Avalonia",
        "Agibuild.Modulus.UI.Blazor",
        // Common framework assemblies
        "Microsoft.Extensions.DependencyInjection.Abstractions",
        "Microsoft.Extensions.Logging.Abstractions",
        "System.",
        "Microsoft.CSharp",
        "netstandard",
    };

    public static Command Create()
    {
        var pathOption = new Option<string?>("--path", "-p")
        {
            Description = "Path to module project directory (default: current directory)"
        };

        var outputOption = new Option<string?>("--output", "-o")
        {
            Description = "Output directory for .modpkg file (default: ./output)"
        };

        var configurationOption = new Option<string?>("--configuration", "-c")
        {
            Description = "Build configuration (Debug/Release, default: Release)"
        };

        var noBuildOption = new Option<bool>("--no-build")
        {
            Description = "Skip building the project (use existing build output)"
        };

        var verboseOption = new Option<bool>("--verbose", "-v")
        {
            Description = "Show detailed output"
        };

        var command = new Command("pack", "Build and package the module into a .modpkg file");
        command.Add(pathOption);
        command.Add(outputOption);
        command.Add(configurationOption);
        command.Add(noBuildOption);
        command.Add(verboseOption);

        command.SetAction(async (parseResult, cancellationToken) =>
        {
            var path = parseResult.GetValue(pathOption);
            var output = parseResult.GetValue(outputOption);
            var configuration = parseResult.GetValue(configurationOption) ?? "Release";
            var noBuild = parseResult.GetValue(noBuildOption);
            var verbose = parseResult.GetValue(verboseOption);
            await HandleAsync(path, output, configuration, noBuild, verbose);
        });

        return command;
    }

    private static async Task HandleAsync(string? path, string? output, string configuration, bool noBuild, bool verbose)
    {
        // Resolve project directory to absolute path
        var projectDir = path != null 
            ? Path.GetFullPath(path) 
            : Directory.GetCurrentDirectory();

        if (!Directory.Exists(projectDir))
        {
            Console.WriteLine($"Error: Directory not found: {projectDir}");
            return;
        }

        // Get module name from directory (used for package naming)
        var moduleName = Path.GetFileName(projectDir);

        // Resolve output directory to absolute path
        // If output is specified, resolve relative to current working directory
        // Otherwise, use <projectDir>/output
        var outputDir = output != null
            ? Path.GetFullPath(output)
            : Path.Combine(projectDir, "output");

        Console.WriteLine($"Packaging module: {moduleName}");
        if (verbose)
        {
            Console.WriteLine($"  Project: {projectDir}");
            Console.WriteLine($"  Output: {outputDir}");
        }

        // Step 1: Find project file
        var (projectFile, projectType) = BuildCommand.FindProjectFile(projectDir);
        if (projectFile == null)
        {
            Console.WriteLine("Error: No module project found.");
            Console.WriteLine("Expected: .sln file or .csproj file with Modulus.Sdk reference");
            return;
        }

        // Step 2: Build if needed
        if (!noBuild)
        {
            Console.WriteLine();
            Console.WriteLine("Step 1: Building project...");
            
            var buildResult = await RunDotnetBuildAsync(projectFile, projectDir, configuration, verbose);
            if (!buildResult)
            {
                Console.WriteLine("Error: Build failed. Fix errors and try again, or use --no-build to skip.");
                return;
            }
            Console.WriteLine("  ✓ Build succeeded");
        }
        else
        {
            Console.WriteLine("  Skipping build (--no-build)");
        }

        // Step 3: Find build output
        Console.WriteLine();
        Console.WriteLine("Step 2: Collecting files...");
        
        var buildOutputDir = FindBuildOutput(projectDir, configuration);
        if (buildOutputDir == null)
        {
            Console.WriteLine("Error: Could not find build output directory.");
            Console.WriteLine($"  Expected: bin/{configuration}/ or similar");
            return;
        }

        if (verbose)
        {
            Console.WriteLine($"  Build output: {buildOutputDir}");
        }

        // Step 4: Find manifest
        var manifestPath = FindManifest(projectDir, buildOutputDir);
        if (manifestPath == null)
        {
            Console.WriteLine("Error: extension.vsixmanifest not found.");
            Console.WriteLine("  Create a manifest file or use 'modulus new' to create a new module.");
            return;
        }

        // Read manifest for module info
        var (moduleId, version, displayName) = ReadManifestInfo(manifestPath);
        if (moduleId == null)
        {
            Console.WriteLine("Error: Could not read module identity from manifest.");
            return;
        }

        // Determine package name: prefer DisplayName, fallback to directory name
        // Sanitize the name for use as filename
        var packageBaseName = SanitizeFileName(displayName ?? moduleName);

        Console.WriteLine($"  Module: {displayName ?? moduleName}");
        Console.WriteLine($"  ID: {moduleId}");
        Console.WriteLine($"  Version: {version}");

        // Step 5: Collect files for packaging
        var filesToPack = CollectFilesForPackaging(projectDir, buildOutputDir, manifestPath, verbose);
        Console.WriteLine($"  Files collected: {filesToPack.Count}");

        // Step 6: Create .modpkg
        Console.WriteLine();
        Console.WriteLine("Step 3: Creating package...");
        
        Directory.CreateDirectory(outputDir);
        var packageName = $"{packageBaseName}-{version}.modpkg";
        var packagePath = Path.Combine(outputDir, packageName);

        if (File.Exists(packagePath))
        {
            File.Delete(packagePath);
        }

        using (var archive = ZipFile.Open(packagePath, ZipArchiveMode.Create))
        {
            foreach (var (sourcePath, entryName) in filesToPack)
            {
                if (verbose)
                {
                    Console.WriteLine($"    Adding: {entryName}");
                }
                archive.CreateEntryFromFile(sourcePath, entryName, CompressionLevel.Optimal);
            }
        }

        var fileInfo = new FileInfo(packagePath);
        Console.WriteLine($"  ✓ Package created: {packageName}");
        Console.WriteLine($"    Size: {FormatFileSize(fileInfo.Length)}");

        Console.WriteLine();
        Console.WriteLine("✓ Packaging complete!");
        Console.WriteLine($"  Output: {packagePath}");
        Console.WriteLine();
        Console.WriteLine("To install this module:");
        Console.WriteLine($"  modulus install \"{packagePath}\"");
    }

    private static async Task<bool> RunDotnetBuildAsync(string projectFile, string workingDir, string configuration, bool verbose)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"build \"{projectFile}\" --configuration {configuration}",
            WorkingDirectory = workingDir,
            RedirectStandardOutput = !verbose,
            RedirectStandardError = !verbose,
            UseShellExecute = false
        };

        using var process = Process.Start(psi);
        if (process == null)
        {
            return false;
        }

        if (!verbose)
        {
            Console.Write("  Building");
            while (!process.HasExited)
            {
                Console.Write(".");
                await Task.Delay(500);
            }
            Console.WriteLine();
        }

        await process.WaitForExitAsync();
        return process.ExitCode == 0;
    }

    private static string? FindBuildOutput(string projectDir, string configuration)
    {
        // Look for multi-project structure (solution with Core/UI projects)
        var coreProjectDir = Directory.GetDirectories(projectDir)
            .FirstOrDefault(d => Path.GetFileName(d).EndsWith(".Core", StringComparison.OrdinalIgnoreCase));

        if (coreProjectDir != null)
        {
            // Multi-project: look for output in Core project
            var coreBinDir = Path.Combine(coreProjectDir, "bin", configuration);
            if (Directory.Exists(coreBinDir))
            {
                // Find the target framework folder
                var tfmDirs = Directory.GetDirectories(coreBinDir);
                if (tfmDirs.Length > 0)
                {
                    return tfmDirs[0];
                }
                return coreBinDir;
            }
        }

        // Single project or direct bin folder
        var binDir = Path.Combine(projectDir, "bin", configuration);
        if (Directory.Exists(binDir))
        {
            var tfmDirs = Directory.GetDirectories(binDir);
            if (tfmDirs.Length > 0)
            {
                return tfmDirs[0];
            }
            return binDir;
        }

        return BuildCommand.FindOutputDirectory(projectDir, configuration);
    }

    private static string? FindManifest(string projectDir, string buildOutputDir)
    {
        // Check build output first
        var manifestInOutput = Path.Combine(buildOutputDir, "extension.vsixmanifest");
        if (File.Exists(manifestInOutput))
        {
            return manifestInOutput;
        }

        // Check project root
        var manifestInRoot = Path.Combine(projectDir, "extension.vsixmanifest");
        if (File.Exists(manifestInRoot))
        {
            return manifestInRoot;
        }

        // Check Core project directory
        var coreProjectDir = Directory.GetDirectories(projectDir)
            .FirstOrDefault(d => Path.GetFileName(d).EndsWith(".Core", StringComparison.OrdinalIgnoreCase));
        
        if (coreProjectDir != null)
        {
            var manifestInCore = Path.Combine(coreProjectDir, "extension.vsixmanifest");
            if (File.Exists(manifestInCore))
            {
                return manifestInCore;
            }
        }

        return null;
    }

    private static (string? Id, string? Version, string? DisplayName) ReadManifestInfo(string manifestPath)
    {
        try
        {
            var doc = XDocument.Load(manifestPath);
            var ns = doc.Root?.GetDefaultNamespace() ?? XNamespace.None;
            
            var identity = doc.Descendants(ns + "Identity").FirstOrDefault();
            var metadata = doc.Descendants(ns + "Metadata").FirstOrDefault();
            
            var id = identity?.Attribute("Id")?.Value;
            var version = identity?.Attribute("Version")?.Value ?? "1.0.0";
            var displayName = metadata?.Element(ns + "DisplayName")?.Value;

            return (id, version, displayName);
        }
        catch
        {
            return (null, null, null);
        }
    }

    private static List<(string SourcePath, string EntryName)> CollectFilesForPackaging(
        string projectDir, 
        string buildOutputDir, 
        string manifestPath,
        bool verbose)
    {
        var files = new List<(string, string)>();
        var addedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // 1. Add manifest
        files.Add((manifestPath, "extension.vsixmanifest"));
        addedFiles.Add("extension.vsixmanifest");

        // 2. Collect DLLs from all project output directories
        var allOutputDirs = new List<string> { buildOutputDir };
        
        // Find other project output directories (UI.Avalonia, UI.Blazor, etc.)
        foreach (var subDir in Directory.GetDirectories(projectDir))
        {
            var subDirName = Path.GetFileName(subDir);
            if (subDirName.Contains(".UI.") || subDirName.EndsWith(".Core"))
            {
                var subBinDir = Path.Combine(subDir, "bin");
                if (Directory.Exists(subBinDir))
                {
                    var configDirs = Directory.GetDirectories(subBinDir, "*", SearchOption.AllDirectories);
                    allOutputDirs.AddRange(configDirs.Where(d => 
                        Directory.GetFiles(d, "*.dll").Length > 0));
                }
            }
        }

        // Collect DLLs
        foreach (var dir in allOutputDirs.Distinct())
        {
            foreach (var dll in Directory.GetFiles(dir, "*.dll"))
            {
                var fileName = Path.GetFileName(dll);
                
                // Skip shared assemblies
                if (IsSharedAssembly(fileName))
                {
                    if (verbose)
                    {
                        Console.WriteLine($"    Skipping shared: {fileName}");
                    }
                    continue;
                }

                if (!addedFiles.Contains(fileName))
                {
                    files.Add((dll, fileName));
                    addedFiles.Add(fileName);
                }
            }
        }

        // 3. Add optional files from project root
        var optionalFiles = new[] { "README.md", "LICENSE.txt", "LICENSE", "CHANGELOG.md" };
        foreach (var optFile in optionalFiles)
        {
            var optPath = Path.Combine(projectDir, optFile);
            if (File.Exists(optPath) && !addedFiles.Contains(optFile))
            {
                files.Add((optPath, optFile));
                addedFiles.Add(optFile);
            }
        }

        return files;
    }

    private static bool IsSharedAssembly(string fileName)
    {
        // Remove .dll extension for comparison
        var name = Path.GetFileNameWithoutExtension(fileName);
        
        foreach (var prefix in SharedAssemblyPrefixes)
        {
            if (name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static string FormatFileSize(long bytes)
    {
        if (bytes < 1024)
            return $"{bytes} B";
        if (bytes < 1024 * 1024)
            return $"{bytes / 1024.0:F1} KB";
        return $"{bytes / (1024.0 * 1024.0):F2} MB";
    }

    /// <summary>
    /// Sanitize a string for use as a filename by removing invalid characters.
    /// </summary>
    private static string SanitizeFileName(string name)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = new string(name.Where(c => !invalidChars.Contains(c)).ToArray());
        
        // Replace spaces with nothing or keep as-is based on preference
        // Also trim any leading/trailing whitespace
        return sanitized.Trim();
    }
}

