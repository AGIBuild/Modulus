using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json;
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
    readonly string PluginName;
    
    [Parameter("Target host to build for: 'avalonia' (default), 'blazor', or 'all'", Name = "target-host")]
    readonly string TargetHost = "avalonia";

    [Solution] readonly Solution Solution;

    AbsolutePath ArtifactsDirectory => RootDirectory / "artifacts";
    AbsolutePath PluginsArtifactsDirectory => ArtifactsDirectory / "plugins";
    AbsolutePath SamplesDirectory => RootDirectory / "src" / "samples";
    AbsolutePath ModulesDirectory => RootDirectory / "src" / "Modules";
    
    // Host project names
    const string AvaloniaHostProject = "Modulus.Host.Avalonia";
    const string BlazorHostProject = "Modulus.Host.Blazor";

    Target Clean => _ => _
        .Executes(() =>
        {
            if (Directory.Exists(ArtifactsDirectory))
                Directory.Delete(ArtifactsDirectory, true);
            var binDirs = Directory.GetDirectories(RootDirectory / "src", "bin", SearchOption.AllDirectories);
            var objDirs = Directory.GetDirectories(RootDirectory / "src", "obj", SearchOption.AllDirectories);
            foreach (var dir in binDirs.Concat(objDirs))
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
            var hostProjectName = TargetHost?.ToLower() == "blazor" ? BlazorHostProject : AvaloniaHostProject;
            var outputDir = ArtifactsDirectory / hostProjectName;
            
            var executable = outputDir / hostProjectName;
            if (OperatingSystem.IsWindows())
                executable = outputDir / $"{hostProjectName}.exe";
            
            if (!File.Exists(executable))
            {
                LogError($"Executable not found: {executable}");
                LogError("Run 'nuke build' first to build the application.");
                throw new Exception($"Executable not found: {executable}");
            }
            
            LogHeader($"Running {hostProjectName}");
            LogHighlight($"Executable: {executable}");
            
            // Run the application
            var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = executable,
                WorkingDirectory = outputDir,
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

    Target BuildAll => _ => _
        .DependsOn(Compile, Test);

    Target Default => _ => _
        .DependsOn(Compile);
    
    // ============================================================
    // Application Build Targets
    // ============================================================
    
    /// <summary>
    /// Just compile the solution (no publish/package)
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
    /// Build and publish host application to artifacts/
    /// Usage: nuke build-app [--target-host avalonia|blazor|all]
    /// </summary>
    Target BuildApp => _ => _
        .DependsOn(Restore)
        .Description("Build and publish host application to artifacts/")
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
                
                var outputDir = ArtifactsDirectory / hostProjectName;
                
                LogHeader($"Building {hostProjectName}");
                
                DotNetTasks.DotNetPublish(s => s
                    .SetProject(hostProject)
                    .SetConfiguration(Configuration)
                    .SetOutput(outputDir)
                    .EnableNoRestore());
                
                LogSuccess($"Published {hostProjectName} to {outputDir}");
            }
        });
    
    /// <summary>
    /// Build and package modules to artifacts/{Host}/Modules/
    /// Usage: nuke build-module [--target-host avalonia|blazor|all] [--name ModuleName]
    /// </summary>
    Target BuildModule => _ => _
        .DependsOn(Restore)
        .Description("Build and package modules to artifacts/{Host}/Modules/")
        .Executes(() =>
        {
            var hostProjects = GetTargetHostProjects();
            
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
                var manifestPath = Path.Combine(moduleDir, "manifest.json");
                
                if (!File.Exists(manifestPath))
                {
                    LogWarning($"No manifest.json in {moduleName}, skipping");
                    continue;
                }
                
                LogHeader($"Building Module: {moduleName}");
                
                // Build all projects in this module
                var moduleProjects = Directory.GetFiles(moduleDir, "*.csproj", SearchOption.AllDirectories);
                foreach (var projectPath in moduleProjects)
                {
                    DotNetTasks.DotNetBuild(s => s
                        .SetProjectFile(projectPath)
                        .SetConfiguration(Configuration)
                        .EnableNoRestore());
                }
                
                // Package to each target host's Modules directory
                foreach (var hostProjectName in hostProjects)
                {
                    var hostType = hostProjectName.Contains("Avalonia") ? "Avalonia" : "Blazor";
                    var moduleOutputDir = ArtifactsDirectory / hostProjectName / "Modules" / moduleName;
                    
                    // Clean and create output directory
                    if (Directory.Exists(moduleOutputDir))
                        Directory.Delete(moduleOutputDir, true);
                    Directory.CreateDirectory(moduleOutputDir);
                    
                    // Copy manifest.json
                    File.Copy(manifestPath, moduleOutputDir / "manifest.json");
                    
                    // Copy DLLs from each project's output
                    foreach (var projectPath in moduleProjects)
                    {
                        var projectDir = Path.GetDirectoryName(projectPath);
                        var projectName = Path.GetFileNameWithoutExtension(projectPath);
                        
                        // Skip UI projects that don't match the host type
                        if (projectName.Contains(".UI."))
                        {
                            var isAvaloniaUi = projectName.Contains(".UI.Avalonia");
                            var isBlazorUi = projectName.Contains(".UI.Blazor");
                            
                            if (isAvaloniaUi && hostType != "Avalonia") continue;
                            if (isBlazorUi && hostType != "Blazor") continue;
                        }
                        
                        // Find the output directory
                        var binDir = Path.Combine(projectDir, "bin", Configuration.ToString());
                        if (!Directory.Exists(binDir)) continue;
                        
                        // Find the target framework folder
                        var tfmDirs = Directory.GetDirectories(binDir);
                        var tfmDir = tfmDirs.FirstOrDefault(d => d.Contains("net"));
                        if (tfmDir == null) continue;
                        
                        // Copy DLL and PDB
                        var dllPath = Path.Combine(tfmDir, $"{projectName}.dll");
                        var pdbPath = Path.Combine(tfmDir, $"{projectName}.pdb");
                        
                        if (File.Exists(dllPath))
                            File.Copy(dllPath, moduleOutputDir / $"{projectName}.dll", true);
                        if (File.Exists(pdbPath))
                            File.Copy(pdbPath, moduleOutputDir / $"{projectName}.pdb", true);
                    }
                    
                    LogSuccess($"  → {hostType}: {moduleOutputDir}");
                }
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
        return TargetHost?.ToLower() switch
        {
            "blazor" => new[] { BlazorHostProject },
            "all" => new[] { AvaloniaHostProject, BlazorHostProject },
            _ => new[] { AvaloniaHostProject }  // default to avalonia
        };
    }

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
}
