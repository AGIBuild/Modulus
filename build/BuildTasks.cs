using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml.Linq;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;
using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using static Nuke.Common.EnvironmentInfo;
using Nuke.Common.Tools.DotNet;

class BuildTasks : NukeBuild
{
    /// Support plugins are available for:
    ///   - JetBrains ReSharper        https://nuke.build/resharper
    ///   - JetBrains Rider            https://nuke.build/rider
    ///   - Microsoft VisualStudio     https://nuke.build/visualstudio
    ///   - Microsoft VSCode           https://nuke.build/vscode

    public static int Main()
    {
        // Configure Serilog with console coloring
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console(theme: AnsiConsoleTheme.Code)
            .CreateLogger();

        return Execute<BuildTasks>(x => x.Compile);
    }

    // Custom log helpers with ANSI color codes for consistent coloring across platforms
    private static void LogSuccess(string message) => Console.WriteLine($"\u001b[32m✓ {message}\u001b[0m");  // Green text
    private static void LogError(string message) => Log.Error($"✗ {message}");  // Red text via Error level
    private static void LogWarning(string message) => Log.Warning($"⚠ {message}");  // Yellow text via Warning level
    private static void LogHighlight(string message) => Log.Information($"→ {message}");  // Regular text with arrow
    private static void LogNormal(string message) => Log.Information($"{message}");  // Regular text
    private static void LogHeader(string message)
    {
        Log.Information("");
        Log.Information(new string('=', 50));
        Log.Information($"🔹 {message} 🔹");
        Log.Information(new string('=', 50));
    }

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    [Parameter("Operation to perform: 'all' (default) or 'single'", Name = "op")]
    readonly string Operation = "all";

    [Parameter("Name of the plugin to pack (required when op=single)", Name = "name")]
    readonly string? PluginName;
    
    [Parameter("Target host to build for: 'avalonia' (default), 'blazor', or 'all'", Name = "app")]
    readonly string TargetHost = "avalonia";

    [Parameter("NuGet source URL (default: nuget.org)", Name = "source")]
    readonly string NuGetSource = "https://api.nuget.org/v3/index.json";

    private string EffectiveTargetHost => (TargetHost ?? "avalonia").ToLower();

    [Solution] readonly Solution Solution = null!;

    AbsolutePath ArtifactsDirectory => RootDirectory / "artifacts";
    AbsolutePath BinDirectory => ArtifactsDirectory / "bin";
    AbsolutePath ModulesBinDirectory => BinDirectory / "Modules";  // Module compilation output
    AbsolutePath CliDirectory => ArtifactsDirectory / "cli";
    AbsolutePath TestsDirectory => ArtifactsDirectory / "tests";
    AbsolutePath ModulesInstallerDirectory => ArtifactsDirectory / "ModulesInstaller";  // .modpkg packages
    AbsolutePath PluginsArtifactsDirectory => ArtifactsDirectory / "plugins";
    AbsolutePath SamplesDirectory => RootDirectory / "src" / "samples";
    AbsolutePath ModulesDirectory => RootDirectory / "src" / "Modules";
    
    // Host project names
    const string AvaloniaHostProject = "Modulus.Host.Avalonia";
    const string BlazorHostProject = "Modulus.Host.Blazor";

    Target Clean => _ => _
        .Executes(() =>
        {
            // Clean artifacts directory (contains all build output and obj files)
            if (Directory.Exists(ArtifactsDirectory))
                Directory.Delete(ArtifactsDirectory, true);
            
            // Clean any legacy bin/obj directories in src (for backwards compatibility)
            var legacyBinDirs = Directory.GetDirectories(RootDirectory / "src", "bin", SearchOption.AllDirectories);
            var legacyObjDirs = Directory.GetDirectories(RootDirectory / "src", "obj", SearchOption.AllDirectories);
            foreach (var dir in legacyBinDirs.Concat(legacyObjDirs))
            {
                Directory.Delete(dir, true);
            }
            
            // Clean legacy bin/obj directories in tests
            var testBinDirs = Directory.GetDirectories(RootDirectory / "tests", "bin", SearchOption.AllDirectories);
            var testObjDirs = Directory.GetDirectories(RootDirectory / "tests", "obj", SearchOption.AllDirectories);
            foreach (var dir in testBinDirs.Concat(testObjDirs))
            {
                Directory.Delete(dir, true);
            }
        });

    Target Restore => _ => _
        .Executes(() =>
        {
            DotNetTasks.DotNetRestore(s => s
                .SetProjectFile(Solution));
        });

    Target Pack => _ => _
        .DependsOn(Compile)
        .Executes(() =>
        {
            foreach (var project in Solution.AllProjects.Where(p => p.Name.Contains("Plugin") || p.Name.Contains("App")))
            {
                DotNetTasks.DotNetPack(s => s
                    .SetProject(project)
                    .SetConfiguration(Configuration)
                    .SetOutputDirectory(ArtifactsDirectory)
                    .EnableNoBuild());
            }
        });

    /// <summary>
    /// Build all and run the host application
    /// Usage: nuke run [--target-host avalonia|blazor]
    /// </summary>
    Target Run => _ => _
        .DependsOn(Build)
        .Description("Build all and run the host application")
        .Executes(() =>
        {
            var hostProjectName = EffectiveTargetHost == "blazor" ? BlazorHostProject : AvaloniaHostProject;
            
            // All binaries are now in artifacts/bin/
            var executable = BinDirectory / hostProjectName;
            if (OperatingSystem.IsWindows())
                executable = BinDirectory / $"{hostProjectName}.exe";
            
            if (!File.Exists(executable))
            {
                LogError($"Executable not found: {executable}");
                LogError("Run 'nuke build' first to build the application.");
                throw new Exception($"Executable not found: {executable}");
            }
            
            LogHeader($"Running {hostProjectName}");
            LogHighlight($"Executable: {executable}");
            
            // Run the application from artifacts/bin directory
            var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = executable,
                WorkingDirectory = BinDirectory,
                UseShellExecute = false
            });
            
            if (process != null)
            {
                LogSuccess($"Started {hostProjectName} (PID: {process.Id})");
                process.WaitForExit();
                LogNormal($"Application exited with code: {process.ExitCode}");
            }
        });

    Target Test => _ => _
        .DependsOn(Compile)
        .Executes(() =>
        {
            DotNetTasks.DotNetTest(s => s
                .SetProjectFile(Solution)
                .SetConfiguration(Configuration)
                .SetNoBuild(true));
        });

    /// <summary>
    /// Run CLI integration tests.
    /// Usage: nuke test-cli
    /// </summary>
    Target TestCli => _ => _
        .DependsOn(Compile)
        .Description("Run CLI integration tests")
        .Executes(() =>
        {
            LogHeader("Running CLI Integration Tests");
            
            var testProject = RootDirectory / "tests" / "Modulus.Cli.IntegrationTests" / "Modulus.Cli.IntegrationTests.csproj";
            
            DotNetTasks.DotNetTest(s => s
                .SetProjectFile(testProject)
                .SetConfiguration(Configuration)
                .SetNoBuild(true));
            
            LogSuccess("CLI integration tests passed");
        });

    Target BuildAll => _ => _
        .DependsOn(Compile, Test);

    Target Default => _ => _
        .DependsOn(Compile);
    
    // ============================================================
    // Application Build Targets
    // ============================================================
    
    /// <summary>
    /// Just compile the solution (default bin/Debug output)
    /// </summary>
    Target Compile => _ => _
        .DependsOn(Restore)
        .Description("Compile the solution")
        .Executes(() =>
        {
            DotNetTasks.DotNetBuild(s => s
                .SetProjectFile(Solution)
                .SetConfiguration(Configuration)
                .EnableNoRestore());
        });
    
    /// <summary>
    /// Build host application to artifacts/
    /// OutputPath is configured in each .csproj file
    /// Usage: nuke build-app [--target-host avalonia|blazor|all]
    /// </summary>
    Target BuildApp => _ => _
        .DependsOn(Restore)
        .Description("Build host application to artifacts/")
        .Executes(() =>
        {
            var hostProjects = GetTargetHostProjects();
            
            foreach (var hostProjectName in hostProjects)
            {
                var hostProject = Solution.AllProjects.FirstOrDefault(p => p.Name == hostProjectName);
                if (hostProject == null)
                {
                    LogError($"Host project not found: {hostProjectName}");
                    continue;
                }
                
                LogHeader($"Building {hostProjectName}");
                
                // Build - OutputPath is configured in .csproj
                DotNetTasks.DotNetBuild(s => s
                    .SetProjectFile(hostProject)
                    .SetConfiguration(Configuration)
                    .EnableNoRestore());
                
                LogSuccess($"Built {hostProjectName} to {BinDirectory}");
            }
        });
    
    /// <summary>
    /// Build modules to artifacts/bin/Modules/{ModuleName}/
    /// OutputPath is configured in each .csproj file
    /// Usage: nuke build-module [--name ModuleName]
    /// </summary>
    Target BuildModule => _ => _
        .DependsOn(Restore)
        .Description("Build modules to artifacts/bin/Modules/{ModuleName}/")
        .Executes(() =>
        {
            // Get module directories to build
            var moduleDirectories = string.IsNullOrEmpty(PluginName)
                ? Directory.GetDirectories(ModulesDirectory).Select(d => (AbsolutePath)d).ToArray()
                : new[] { ModulesDirectory / PluginName };
            
            foreach (var moduleDir in moduleDirectories)
            {
                if (!Directory.Exists(moduleDir))
                {
                    LogWarning($"Module directory not found: {moduleDir}");
                    continue;
                }
                
                var moduleName = Path.GetFileName(moduleDir);
                
                // Support both new vsixmanifest and legacy manifest.json
                var vsixManifestPath = Path.Combine(moduleDir, "extension.vsixmanifest");
                var legacyManifestPath = Path.Combine(moduleDir, "manifest.json");
                var manifestPath = File.Exists(vsixManifestPath) ? vsixManifestPath : legacyManifestPath;
                
                if (!File.Exists(manifestPath))
                {
                    LogWarning($"No extension.vsixmanifest or manifest.json in {moduleName}, skipping");
                    continue;
                }
                
                LogHeader($"Building Module: {moduleName}");
                
                var moduleOutputDir = ModulesBinDirectory / moduleName;
                
                // Build all projects - OutputPath is configured in .csproj
                var moduleProjects = Directory.GetFiles(moduleDir, "*.csproj", SearchOption.AllDirectories);
                foreach (var projectPath in moduleProjects)
                {
                    DotNetTasks.DotNetBuild(s => s
                        .SetProjectFile(projectPath)
                        .SetConfiguration(Configuration)
                        .EnableNoRestore());
                }
                
                // Copy manifest to output directory
                var outputManifestPath = moduleOutputDir / Path.GetFileName(manifestPath);
                if (!File.Exists(outputManifestPath) || 
                    File.GetLastWriteTimeUtc(manifestPath) > File.GetLastWriteTimeUtc(outputManifestPath))
                {
                    Directory.CreateDirectory(moduleOutputDir);
                    File.Copy(manifestPath, outputManifestPath, overwrite: true);
                    Log.Information($"Copied {Path.GetFileName(manifestPath)} to {moduleOutputDir}");
                }
                
                LogSuccess($"Built {moduleName} to {moduleOutputDir}");
            }
        });
    
    /// <summary>
    /// Full build: host application + all modules
    /// Usage: nuke build [--target-host avalonia|blazor|all]
    /// </summary>
    Target Build => _ => _
        .DependsOn(BuildApp, BuildModule)
        .Description("Full build: host application + all modules");
    
    // Helper to get target host projects based on --target-host parameter
    private string[] GetTargetHostProjects()
    {
        return EffectiveTargetHost switch
        {
            "blazor" => new[] { BlazorHostProject },
            "all" => new[] { AvaloniaHostProject, BlazorHostProject },
            _ => new[] { AvaloniaHostProject }  // default to avalonia
        };
    }

    AbsolutePath PackagesDirectory => ArtifactsDirectory / "packages";

    // Shared assemblies that should NOT be included in module packages
    private static readonly HashSet<string> SharedAssemblyPrefixes = new(StringComparer.OrdinalIgnoreCase)
    {
        "Modulus.Core",
        "Modulus.Sdk",
        "Modulus.UI.Abstractions",
        "Modulus.UI.Avalonia",
        "Modulus.UI.Blazor",
        "Modulus.Infrastructure.Data"
    };

    private static readonly HashSet<string> FrameworkPrefixes = new(StringComparer.OrdinalIgnoreCase)
    {
        "System.",
        "Microsoft.Extensions.",
        "Microsoft.EntityFrameworkCore.",
        "Microsoft.AspNetCore.",
        "Microsoft.CSharp",
        "mscorlib",
        "netstandard",
        "WindowsBase",
        "PresentationCore",
        "PresentationFramework"
    };

    private static bool IsSharedAssembly(string fileName)
    {
        var name = Path.GetFileNameWithoutExtension(fileName);
        
        // Check exact matches for shared assemblies
        if (SharedAssemblyPrefixes.Contains(name))
            return true;
        
        // Check framework prefixes
        foreach (var prefix in FrameworkPrefixes)
        {
            if (name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        
        return false;
    }

    /// <summary>
    /// Reads version from extension.vsixmanifest
    /// </summary>
    private static string? ReadManifestVersion(string manifestPath)
    {
        if (!File.Exists(manifestPath))
            return null;
        
        try
        {
            var doc = System.Xml.Linq.XDocument.Load(manifestPath);
            var ns = System.Xml.Linq.XNamespace.Get("http://schemas.microsoft.com/developer/vsx-schema/2011");
            var identity = doc.Root?.Element(ns + "Metadata")?.Element(ns + "Identity")
                        ?? doc.Root?.Element("Metadata")?.Element("Identity");
            return (string?)identity?.Attribute("Version");
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Package modules into .modpkg files
    /// Usage: nuke pack-module [--name ModuleName]
    /// </summary>
    Target PackModule => _ => _
        .DependsOn(BuildModule)
        .Description("Package modules into .modpkg files for distribution")
        .Executes(() =>
        {
            // Ensure modules installer directory exists
            Directory.CreateDirectory(ModulesInstallerDirectory);

            // Get module directories to pack
            var moduleDirectories = string.IsNullOrEmpty(PluginName)
                ? Directory.GetDirectories(ModulesDirectory).Select(d => (AbsolutePath)d).ToArray()
                : new[] { ModulesDirectory / PluginName };

            int successCount = 0;
            int failureCount = 0;
            var packagedModules = new List<(string Name, string Path)>();

            foreach (var moduleDir in moduleDirectories)
            {
                if (!Directory.Exists(moduleDir))
                {
                    LogWarning($"Module directory not found: {moduleDir}");
                    failureCount++;
                    continue;
                }

                var moduleName = Path.GetFileName(moduleDir);
                var manifestPath = Path.Combine(moduleDir, "extension.vsixmanifest");

                if (!File.Exists(manifestPath))
                {
                    LogWarning($"No extension.vsixmanifest in {moduleName}, skipping");
                    failureCount++;
                    continue;
                }

                // Read version from manifest
                var version = ReadManifestVersion(manifestPath) ?? "1.0.0";
                var packageFileName = $"{moduleName}-{version}.modpkg";
                var packagePath = ModulesInstallerDirectory / packageFileName;

                LogHeader($"Packaging Module: {moduleName} v{version}");

                try
                {
                    // Create temp directory for package contents
                    var tempDir = Path.Combine(Path.GetTempPath(), $"modpkg-{moduleName}-{Guid.NewGuid():N}");
                    Directory.CreateDirectory(tempDir);

                    try
                    {
                        // Find and publish all module projects
                        var moduleProjects = Directory.GetFiles(moduleDir, "*.csproj", SearchOption.AllDirectories);
                        var publishedDlls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                        foreach (var projectPath in moduleProjects)
                        {
                            var projectName = Path.GetFileNameWithoutExtension(projectPath);
                            var publishDir = Path.Combine(tempDir, "publish", projectName);

                            // Publish the project
                            DotNetTasks.DotNetPublish(s => s
                                .SetProject(projectPath)
                                .SetConfiguration(Configuration)
                                .SetOutput(publishDir)
                                .EnableNoBuild()
                                .SetProperty("PublishSingleFile", "false")
                                .SetProperty("SelfContained", "false"));

                            // Copy non-shared DLLs to package root
                            foreach (var file in Directory.GetFiles(publishDir, "*.*", SearchOption.TopDirectoryOnly))
                            {
                                var fileName = Path.GetFileName(file);
                                var ext = Path.GetExtension(file).ToLowerInvariant();

                                // Skip non-assembly files except for certain types
                                if (ext != ".dll" && ext != ".pdb")
                                    continue;

                                // Skip shared assemblies
                                if (IsSharedAssembly(fileName))
                                {
                                    Log.Debug($"Skipping shared assembly: {fileName}");
                                    continue;
                                }

                                // Skip if already copied
                                if (publishedDlls.Contains(fileName))
                                    continue;

                                var destPath = Path.Combine(tempDir, fileName);
                                File.Copy(file, destPath, overwrite: true);
                                publishedDlls.Add(fileName);
                            }
                        }

                        // Copy manifest
                        File.Copy(manifestPath, Path.Combine(tempDir, "extension.vsixmanifest"), overwrite: true);

                        // Copy optional files if they exist
                        var optionalFiles = new[] { "README.md", "LICENSE.txt", "LICENSE", "CHANGELOG.md" };
                        foreach (var optFile in optionalFiles)
                        {
                            var srcPath = Path.Combine(moduleDir, optFile);
                            if (File.Exists(srcPath))
                            {
                                File.Copy(srcPath, Path.Combine(tempDir, optFile), overwrite: true);
                            }
                        }

                        // Remove the publish subdirectory
                        var publishSubDir = Path.Combine(tempDir, "publish");
                        if (Directory.Exists(publishSubDir))
                            Directory.Delete(publishSubDir, true);

                        // Create ZIP package
                        if (File.Exists(packagePath))
                            File.Delete(packagePath);

                        System.IO.Compression.ZipFile.CreateFromDirectory(tempDir, packagePath);

                        LogSuccess($"Created package: {packagePath}");
                        packagedModules.Add((moduleName, packagePath));
                        successCount++;
                    }
                    finally
                    {
                        // Clean up temp directory
                        if (Directory.Exists(tempDir))
                            Directory.Delete(tempDir, true);
                    }
                }
                catch (Exception ex)
                {
                    LogError($"Failed to package {moduleName}: {ex.Message}");
                    failureCount++;
                }
            }

            // Print summary
            LogHeader("MODULE PACKAGING SUMMARY");
            LogNormal($"Total modules processed: {successCount + failureCount}");
            if (successCount > 0)
                LogSuccess($"Successfully packaged: {successCount}");
            if (failureCount > 0)
                LogError($"Failed to package: {failureCount}");

            if (packagedModules.Count > 0)
            {
                LogNormal("");
                LogSuccess("Packaged modules:");
                foreach (var (name, path) in packagedModules)
                {
                    LogSuccess($"  ✓ {name} → {Path.GetFileName(path)}");
                }
            }

            LogNormal("");
            LogNormal("Packages output directory:");
            LogHighlight($"  {ModulesInstallerDirectory}");
        });

    /// <summary>
    /// Pack CLI as a dotnet tool NuGet package.
    /// Requires CLI integration tests to pass first.
    /// Usage: nuke pack-cli
    /// </summary>
    Target PackCli => _ => _
        .DependsOn(TestCli)
        .Description("Pack CLI as a dotnet tool NuGet package (runs tests first)")
        .Executes(() =>
        {
            var cliProject = Solution.AllProjects.FirstOrDefault(p => p.Name == "Modulus.Cli");
            if (cliProject == null)
            {
                LogError("Modulus.Cli project not found");
                return;
            }

            LogHeader("Packing Modulus CLI");

            DotNetTasks.DotNetPack(s => s
                .SetProject(cliProject)
                .SetConfiguration(Configuration)
                .SetOutputDirectory(PackagesDirectory)
                .EnableNoBuild());

            LogSuccess($"CLI package created in {PackagesDirectory}");
        });

    Target PackLibs => _ => _
        .DependsOn(Compile)
        .Description("Pack core libraries (Sdk, UI.Abstractions, UI.Avalonia, UI.Blazor)")
        .Executes(() =>
        {
            var libs = new[] { "Modulus.Sdk", "Modulus.UI.Abstractions", "Modulus.UI.Avalonia", "Modulus.UI.Blazor" };
            
            foreach (var libName in libs)
            {
                var project = Solution.AllProjects.FirstOrDefault(p => p.Name == libName);
                if (project == null)
                {
                    LogWarning($"Project {libName} not found");
                    continue;
                }

                LogHeader($"Packing {libName}");
                
                DotNetTasks.DotNetPack(s => s
                    .SetProject(project)
                    .SetConfiguration(Configuration)
                    .SetOutputDirectory(PackagesDirectory)
                    .EnableNoBuild());
            }
            
            LogSuccess($"Core libraries packaged in {PackagesDirectory}");
        });

    Target PublishLibs => _ => _
        .DependsOn(PackLibs)
        .Description("Publish core libraries to NuGet (requires NUGET_API_KEY env var)")
        .Executes(() =>
        {
            var apiKey = Environment.GetEnvironmentVariable("NUGET_API_KEY");
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                LogError("NUGET_API_KEY environment variable is not set");
                throw new Exception("Missing NUGET_API_KEY environment variable");
            }

            var packages = Directory.GetFiles(PackagesDirectory, "Agibuild.Modulus.*.nupkg")
                .Where(p => !Path.GetFileName(p).Contains(".Cli.")) // Exclude CLI
                .ToList();

            if (packages.Count == 0)
            {
                LogError("No library packages found. Run 'nuke pack-libs' first.");
                return;
            }

            LogHeader($"Publishing {packages.Count} packages to {NuGetSource}");

            foreach (var package in packages)
            {
                LogHighlight($"Pushing: {Path.GetFileName(package)}");
                
                DotNetTasks.DotNetNuGetPush(s => s
                    .SetTargetPath(package)
                    .SetSource(NuGetSource)
                    .SetApiKey(apiKey)
                    .EnableSkipDuplicate());
            }
            
            LogSuccess("All libraries published successfully");
        });

    /// <summary>
    /// Publish CLI to NuGet.org
    /// Usage: nuke publish-cli
    /// Requires: NUGET_API_KEY environment variable
    /// </summary>
    Target PublishCli => _ => _
        .DependsOn(PackCli)
        .Description("Publish CLI to NuGet.org (requires NUGET_API_KEY env var)")
        .Executes(() =>
        {
            var apiKey = Environment.GetEnvironmentVariable("NUGET_API_KEY");
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                LogError("NUGET_API_KEY environment variable is not set");
                throw new Exception("Missing NUGET_API_KEY environment variable");
            }

            var packages = Directory.GetFiles(PackagesDirectory, "Agibuild.Modulus.Cli.*.nupkg");
            if (packages.Length == 0)
            {
                LogError("No CLI package found. Run 'nuke pack-cli' first.");
                return;
            }

            var latestPackage = packages.OrderByDescending(File.GetLastWriteTime).First();
            
            LogHeader("Publishing CLI to NuGet.org");
            LogHighlight($"Package: {Path.GetFileName(latestPackage)}");

            DotNetTasks.DotNetNuGetPush(s => s
                .SetTargetPath(latestPackage)
                .SetSource("https://api.nuget.org/v3/index.json")
                .SetApiKey(apiKey));

            LogSuccess($"Published {Path.GetFileName(latestPackage)} to NuGet.org");
        });

    // ============================================================
    // Local NuGet Source Management
    // ============================================================
    
    const string LocalSourceName = "LocalModulus";
    AbsolutePath LocalPackagesDirectory => PackagesDirectory;

    /// <summary>
    /// Add local NuGet source for development testing
    /// Usage: nuke add-local-source
    /// </summary>
    Target AddLocalSource => _ => _
        .Description("Add local NuGet source for development testing")
        .Executes(() =>
        {
            LogHeader("Adding Local NuGet Source");
            
            // Ensure packages directory exists
            Directory.CreateDirectory(LocalPackagesDirectory);
            
            // Check if source already exists
            try
            {
                var result = DotNetTasks.DotNet($"nuget list source", logOutput: false);
                if (result.Any(x => x.Text.Contains(LocalSourceName)))
                {
                    LogWarning($"Source '{LocalSourceName}' already exists");
                    LogHighlight($"Path: {LocalPackagesDirectory}");
                    return;
                }
            }
            catch
            {
                // Ignore errors when listing sources
            }
            
            // Add the source
            DotNetTasks.DotNet($"nuget add source \"{LocalPackagesDirectory}\" --name {LocalSourceName}");
            
            LogSuccess($"Added local NuGet source: {LocalSourceName}");
            LogHighlight($"Path: {LocalPackagesDirectory}");
            LogNormal("");
            LogNormal("You can now reference local packages in your projects.");
            LogNormal("Run 'nuke pack-local' to build and publish packages to this source.");
        });

    /// <summary>
    /// Remove local NuGet source
    /// Usage: nuke remove-local-source
    /// </summary>
    Target RemoveLocalSource => _ => _
        .Description("Remove local NuGet source")
        .Executes(() =>
        {
            LogHeader("Removing Local NuGet Source");
            
            try
            {
                DotNetTasks.DotNet($"nuget remove source {LocalSourceName}");
                LogSuccess($"Removed local NuGet source: {LocalSourceName}");
            }
            catch (Exception ex)
            {
                LogWarning($"Source '{LocalSourceName}' may not exist: {ex.Message}");
            }
        });

    /// <summary>
    /// Pack libraries and make them available in local source (one-step command)
    /// Usage: nuke pack-local [--version 1.2.3]
    /// </summary>
    [Parameter("Package version (default: auto-generated)", Name = "version")]
    readonly string? PackageVersion;

    Target PackLocal => _ => _
        .DependsOn(Compile)
        .Description("Pack core libraries to local NuGet source for development")
        .Executes(() =>
        {
            LogHeader("Packing Libraries to Local Source");
            
            // Ensure packages directory exists
            Directory.CreateDirectory(LocalPackagesDirectory);
            
            // Clean old packages
            var oldPackages = Directory.GetFiles(LocalPackagesDirectory, "Agibuild.Modulus.*.nupkg");
            foreach (var pkg in oldPackages)
            {
                File.Delete(pkg);
                Log.Debug($"Removed old package: {Path.GetFileName(pkg)}");
            }
            
            var libs = new[] { "Modulus.UI.Abstractions", "Modulus.Sdk", "Modulus.UI.Avalonia", "Modulus.UI.Blazor" };
            
            foreach (var libName in libs)
            {
                var project = Solution.AllProjects.FirstOrDefault(p => p.Name == libName);
                if (project == null)
                {
                    LogWarning($"Project {libName} not found");
                    continue;
                }

                LogHighlight($"Packing {libName}...");
                
                DotNetTasks.DotNetPack(s => s
                    .SetProject(project)
                    .SetConfiguration(Configuration)
                    .SetOutputDirectory(LocalPackagesDirectory)
                    .EnableNoBuild());
                
                // Find the created package to show version
                var createdPackage = Directory.GetFiles(LocalPackagesDirectory, $"{libName}.*.nupkg")
                    .OrderByDescending(File.GetLastWriteTime)
                    .FirstOrDefault();
                    
                if (createdPackage != null)
                {
                    var version = Path.GetFileNameWithoutExtension(createdPackage)
                        .Replace($"{libName}.", "");
                    LogSuccess($"  ✓ {libName} v{version}");
                }
            }
            
            LogHeader("LOCAL PACKAGES READY");
            LogNormal($"Packages directory: {LocalPackagesDirectory}");
            LogNormal("");
            
            // Check if local source is configured
            try
            {
                var result = DotNetTasks.DotNet($"nuget list source", logOutput: false);
                if (!result.Any(x => x.Text.Contains(LocalSourceName)))
                {
                    LogWarning($"Local source '{LocalSourceName}' not configured.");
                    LogNormal("Run 'nuke add-local-source' to add it.");
                }
                else
                {
                    LogSuccess($"Local source '{LocalSourceName}' is configured.");
                }
            }
            catch
            {
                // Ignore
            }
            
            LogNormal("");
            LogNormal("To use these packages in module templates:");
            LogNormal("  1. Run 'nuke add-local-source' (if not done)");
            LogNormal("  2. Run 'modulus new -n MyModule'");
            LogNormal("  3. dotnet restore will find packages from local source");
        });

    Target CleanPluginsArtifacts => _ => _
        .Executes(() =>
        {
            if (Directory.Exists(PluginsArtifactsDirectory))
            {
                LogNormal($"Cleaning plugins artifacts directory: {PluginsArtifactsDirectory}");
                Directory.Delete(PluginsArtifactsDirectory, true);
            }

            Directory.CreateDirectory(PluginsArtifactsDirectory);
        });

    // Unified Plugin target that handles both all and single modes
    Target Plugin => _ => _
        .DependsOn(Build, CleanPluginsArtifacts)
        .Description("Manages plugin packaging. Usage: nuke plugin [--op all|single] [--name PluginName]")
        .Executes(() =>
        {
            switch (Operation.ToLower())
            {
                case "single":
                    if (string.IsNullOrEmpty(PluginName))
                    {
                        LogError("Plugin name must be specified when using --op single");
                        LogError("Usage: nuke plugin --op single --name PluginName");
                        throw new Exception("Missing required parameter: --name");
                    }

                    LogNormal($"Packaging single plugin: {PluginName}");
                    bool success = PackSinglePlugin(PluginName);

                    if (success)
                    {
                        LogSuccess($"Successfully packaged plugin: {PluginName}");
                    }
                    else
                    {
                        LogError($"Failed to package plugin: {PluginName}");
                    }
                    break;

                case "all":
                default:
                    LogNormal("Packaging all sample plugins...");
                    PackAllPlugins();
                    break;
            }
        });

    // Helper method to pack all plugins
    private void PackAllPlugins()
    {
        // Get all plugin directories
        var pluginDirectories = Directory.GetDirectories(SamplesDirectory);

        // Summary tracking
        int successCount = 0;
        int failureCount = 0;
        var successPlugins = new List<string>();
        var failedPlugins = new List<string>();

        // Process each plugin
        foreach (var pluginDirectory in pluginDirectories)
        {
            var pluginName = Path.GetFileName(pluginDirectory);
            LogNormal($"Packaging plugin: {pluginName}");

            if (PackSinglePlugin(pluginName))
            {
                successCount++;
                successPlugins.Add(pluginName);
            }
            else
            {
                failureCount++;
                failedPlugins.Add(pluginName);
            }
        }

        // Print summary report
        PrintPluginSummary(successCount, failureCount, successPlugins, failedPlugins);
    }

    // Helper method to print plugin packaging summary
    private void PrintPluginSummary(int successCount, int failureCount, List<string> successPlugins, List<string> failedPlugins)
    {
        LogHeader("PLUGIN PACKAGING SUMMARY");
        LogNormal($"Total plugins processed: {successCount + failureCount}");

        if (successCount > 0)
        {
            LogSuccess($"Successfully packaged:   {successCount}");
        }
        else
        {
            LogNormal($"Successfully packaged:   {successCount}");
        }

        if (failureCount > 0)
        {
            LogError($"Failed to package:       {failureCount}");
        }
        else
        {
            LogNormal($"Failed to package:       {failureCount}");
        }

        if (successCount > 0)
        {
            LogNormal("");
            LogSuccess("Successful plugins:");
            foreach (var plugin in successPlugins)
            {
                LogSuccess($"  ✓ {plugin}");
            }
        }

        if (failureCount > 0)
        {
            LogNormal("");
            LogError("Failed plugins:");
            foreach (var plugin in failedPlugins)
            {
                LogError($"  ✗ {plugin}");
            }
        }

        LogNormal("");
        LogNormal("Plugins output directory:");
        LogHighlight($"  {PluginsArtifactsDirectory}");
        LogNormal(new string('=', 50));

        // Log summary message for CI/CD environments
        if (failureCount > 0)
        {
            LogWarning("Some plugins failed to package. See summary above for details.");
        }
        else
        {
            LogSuccess("All plugins packaged successfully!");
        }
    }

    // Helper method to pack a single plugin
    private bool PackSinglePlugin(string pluginName)
    {
        try
        {
            // Get the plugin directory
            var pluginDirectory = SamplesDirectory / pluginName;

            if (!Directory.Exists(pluginDirectory))
            {
                LogError($"Plugin directory not found: {pluginDirectory}");
                return false;
            }

            // Find the .csproj file
            var csprojFile = Directory.GetFiles(pluginDirectory, "*.csproj").FirstOrDefault();
            if (csprojFile == null)
            {
                LogWarning($"No .csproj file found in {pluginDirectory}");
                return false;
            }

            // First, just build the project to see if it compiles
            try
            {
                DotNetTasks.DotNetBuild(s => s
                    .SetProjectFile(csprojFile)
                    .SetConfiguration(Configuration));
            }
            catch (Exception ex)
            {
                LogError($"Failed to build plugin {pluginName}: {ex.Message}");
                return false;
            }

            // If build succeeds, publish the plugin
            AbsolutePath outputDir = PluginsArtifactsDirectory / pluginName;

            // Ensure the plugin's output directory is clean before publishing
            if (Directory.Exists(outputDir))
            {
                Log.Debug($"Cleaning output directory for {pluginName}: {outputDir}");
                Directory.Delete(outputDir, true);
            }
            Directory.CreateDirectory(outputDir);

            DotNetTasks.DotNetPublish(s => s
                .SetProject(csprojFile)
                .SetConfiguration(Configuration)
                .SetOutput(outputDir)
                .EnableNoBuild()
                .EnableNoRestore());

            // Create a zip file of the plugin
            var zipFilePath = $"{PluginsArtifactsDirectory}/{pluginName}.zip";
            if (File.Exists(zipFilePath))
                File.Delete(zipFilePath);

            System.IO.Compression.ZipFile.CreateFromDirectory(outputDir, zipFilePath);

            LogSuccess($"Successfully packaged {pluginName} to {zipFilePath}");
            return true;
        }
        catch (Exception ex)
        {
            LogError($"Failed to package {pluginName}: {ex.Message}");

            // Clean up any partially created output for this plugin
            try
            {
                var outputDir = PluginsArtifactsDirectory / pluginName;
                if (Directory.Exists(outputDir))
                    Directory.Delete(outputDir, true);

                var zipFile = $"{PluginsArtifactsDirectory}/{pluginName}.zip";
                if (File.Exists(zipFile))
                    File.Delete(zipFile);
            }
            catch
            {
                // Ignore cleanup errors
            }

            return false;
        }
    }

    // ============================================================
    // Template Packaging Targets
    // ============================================================

    AbsolutePath TemplatesDirectory => RootDirectory / "templates";
    AbsolutePath DotnetNewTemplatesDirectory => TemplatesDirectory / "DotnetNew";

    /// <summary>
    /// Pack dotnet new templates as NuGet package
    /// Usage: nuke pack-templates
    /// </summary>
    Target PackTemplates => _ => _
        .Description("Pack dotnet new templates as NuGet package")
        .Executes(() =>
        {
            LogHeader("Packing dotnet new Templates");
            
            Directory.CreateDirectory(PackagesDirectory);
            
            DotNetTasks.DotNetPack(s => s
                .SetProject(DotnetNewTemplatesDirectory / "Agibuild.Modulus.Templates.csproj")
                .SetConfiguration(Configuration)
                .SetOutputDirectory(PackagesDirectory));
            
            LogSuccess($"Template package created in {PackagesDirectory}");
            LogNormal("");
            LogNormal("To install locally:");
            LogNormal($"  dotnet new install Agibuild.Modulus.Templates --add-source {PackagesDirectory}");
            LogNormal("");
            LogNormal("To publish to NuGet:");
            LogNormal("  dotnet nuget push <package>.nupkg -k <api-key> -s https://api.nuget.org/v3/index.json");
        });

    /// <summary>
    /// Publish dotnet new templates to NuGet
    /// Usage: nuke publish-templates
    /// </summary>
    Target PublishTemplates => _ => _
        .DependsOn(PackTemplates)
        .Description("Publish dotnet new templates to NuGet (requires NUGET_API_KEY env var)")
        .Executes(() =>
        {
            var apiKey = Environment.GetEnvironmentVariable("NUGET_API_KEY");
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                LogError("NUGET_API_KEY environment variable is not set");
                throw new Exception("Missing NUGET_API_KEY environment variable");
            }

            var packages = Directory.GetFiles(PackagesDirectory, "Agibuild.Modulus.Templates.*.nupkg");
            if (packages.Length == 0)
            {
                LogError("No template package found. Run 'nuke pack-templates' first.");
                return;
            }

            var latestPackage = packages.OrderByDescending(File.GetLastWriteTime).First();
            
            LogHeader("Publishing Templates to NuGet.org");
            LogHighlight($"Package: {Path.GetFileName(latestPackage)}");

            DotNetTasks.DotNetNuGetPush(s => s
                .SetTargetPath(latestPackage)
                .SetSource(NuGetSource)
                .SetApiKey(apiKey)
                .EnableSkipDuplicate());

            LogSuccess($"Published {Path.GetFileName(latestPackage)} to NuGet.org");
        });

    /// <summary>
    /// Pack VSIX extension (Windows only)
    /// Usage: nuke pack-vsix
    /// </summary>
    Target PackVsix => _ => _
        .OnlyWhenStatic(() => OperatingSystem.IsWindows())
        .Description("Pack VSIX extension for Visual Studio (Windows only)")
        .Executes(() =>
        {
            LogHeader("Packing VSIX Extension");
            
            var vsixProjectDir = TemplatesDirectory / "VSIX";
            var vsTemplatesDir = TemplatesDirectory / "VisualStudio";
            var projectTemplatesDir = vsixProjectDir / "ProjectTemplates";
            
            Directory.CreateDirectory(projectTemplatesDir);
            
            // Create template ZIP files
            var avaloniaZip = projectTemplatesDir / "ModulusModule.Avalonia.zip";
            var blazorZip = projectTemplatesDir / "ModulusModule.Blazor.zip";
            
            if (File.Exists(avaloniaZip)) File.Delete(avaloniaZip);
            if (File.Exists(blazorZip)) File.Delete(blazorZip);
            
            System.IO.Compression.ZipFile.CreateFromDirectory(
                vsTemplatesDir / "ModulusModule.Avalonia", 
                avaloniaZip);
            LogSuccess("Created ModulusModule.Avalonia.zip");
            
            System.IO.Compression.ZipFile.CreateFromDirectory(
                vsTemplatesDir / "ModulusModule.Blazor", 
                blazorZip);
            LogSuccess("Created ModulusModule.Blazor.zip");
            
            // Build VSIX project
            DotNetTasks.DotNetBuild(s => s
                .SetProjectFile(vsixProjectDir / "Modulus.Templates.Vsix.csproj")
                .SetConfiguration(Configuration));
            
            LogSuccess("VSIX built successfully");
            LogNormal($"Output: {vsixProjectDir / "bin" / Configuration}");
        });
}
