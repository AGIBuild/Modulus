using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;
using System.Text.RegularExpressions;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using Nuke.Common;
using Nuke.Common.CI;
using Nuke.Common.Execution;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Utilities.Collections;
using static Nuke.Common.EnvironmentInfo;
using static Nuke.Common.IO.PathConstruction;
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

        return Execute<BuildTasks>(x => x.BuildAll);
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

    [Parameter("Role to filter context (e.g., Backend, Frontend, Plugin)", Name = "role")]
    readonly string Role;

    [Parameter("Verbosity for ManifestSync tool (true/false)", Name = "verbose")]
    readonly bool Verbose = false;

    [Solution] readonly Solution Solution;

    AbsolutePath ArtifactsDirectory => RootDirectory / "artifacts";
    AbsolutePath PluginsArtifactsDirectory => ArtifactsDirectory / "plugins";
    AbsolutePath SamplesDirectory => RootDirectory / "src" / "samples";
    AbsolutePath AiManifestFile => RootDirectory / "ai-manifest.yaml";
    AbsolutePath DocsDirectory => RootDirectory / "docs";
    AbsolutePath EnglishDocsDirectory => DocsDirectory / "en-US";
    AbsolutePath ChineseDocsDirectory => DocsDirectory / "zh-CN";
    AbsolutePath ReportsDirectory => DocsDirectory / "reports";

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

    Target Build => _ => _
        .DependsOn(Restore)
        .Executes(() =>
        {
            DotNetTasks.DotNetBuild(s => s
                .SetProjectFile(Solution)
                .SetConfiguration(Configuration)
                .EnableNoRestore());
        });

    Target Pack => _ => _
        .DependsOn(Build)
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

    Target Run => _ => _
        .Executes(() =>
        {
            var desktopProject = Solution.AllProjects.FirstOrDefault(p => p.Name == "Modulus.App.Desktop");
            if (desktopProject == null)
                throw new Exception("Modulus.App.Desktop project not found");
            DotNetTasks.DotNetRun(s => s
                .SetProjectFile(desktopProject)
                .SetConfiguration(Configuration));
        });

    Target Test => _ => _
        .DependsOn(Build)
        .Executes(() =>
        {
            DotNetTasks.DotNetTest(s => s
                .SetProjectFile(Solution)
                .SetConfiguration(Configuration)
                .SetNoBuild(true));
        });

    Target BuildAll => _ => _
        .DependsOn(Build, Test);

    Target Default => _ => _
        .DependsOn(Build);

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

    Target StartAI => _ => _
        .Description("Generates project context for AI assistance (GitHub Copilot). Use --role to filter for specific context.")
        .Executes(() =>
        {
            LogHeader("🤖 MODULUS AI CONTEXT BOOTSTRAP");
            LogNormal("Gathering project context for AI tools (GitHub Copilot)...");

            // Check if manifest exists
            if (!File.Exists(AiManifestFile))
            {
                LogError("AI manifest file not found at: " + AiManifestFile);
                throw new Exception("Missing ai-manifest.yaml file");
            }

            // Load manifest
            var yamlContent = File.ReadAllText(AiManifestFile);
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .IgnoreUnmatchedProperties()
                .Build();

            var manifest = deserializer.Deserialize<Dictionary<string, object>>(yamlContent);

            // Load progress report if available
            string progressReport = "";
            var progressReportFile = ReportsDirectory / "story-progress-report.en-US.md";
            if (File.Exists(progressReportFile))
            {
                progressReport = File.ReadAllText(progressReportFile);
                // Extract just the content section for brevity
                var match = Regex.Match(progressReport, @"# Modulus Project Progress.*?```", RegexOptions.Singleline);
                if (match.Success)
                {
                    progressReport = match.Value;
                }
                else
                {
                    progressReport = "# Modulus Project Progress\nNo detailed progress information available.";
                }
            }

            // Filter based on role if specified
            if (!string.IsNullOrEmpty(Role))
            {
                LogHighlight($"Filtering context for role: {Role}");
            }

            // Generate AI context output
            var sb = new StringBuilder();
            sb.AppendLine("# 🌟 MODULUS PROJECT CONTEXT");

            // Project Overview
            if (manifest.TryGetValue("projectInfo", out var projInfoObj))
            {
                var projInfo = projInfoObj as Dictionary<object, object>;
                sb.AppendLine("## Project Overview");
                if (projInfo != null)
                {
                    if (projInfo.TryGetValue("name", out var name))
                        sb.AppendLine($"**Project Name:** {name}");

                    if (projInfo.TryGetValue("vision", out var vision))
                        sb.AppendLine($"**Vision:** {vision}");

                    if (projInfo.TryGetValue("description", out var desc))
                        sb.AppendLine($"**Description:** {desc}");

                    if (projInfo.TryGetValue("languages", out var langObj))
                    {
                        var languages = langObj as List<object>;
                        if (languages?.Count > 0)
                            sb.AppendLine($"**Languages:** {string.Join(", ", languages)}");
                    }

                    if (projInfo.TryGetValue("frameworks", out var fwObj))
                    {
                        var frameworks = fwObj as List<object>;
                        if (frameworks?.Count > 0)
                            sb.AppendLine($"**Frameworks:** {string.Join(", ", frameworks)}");
                    }
                }
            }

            // Role-specific context section
            if (!string.IsNullOrEmpty(Role))
            {
                sb.AppendLine($"## Role-Specific Context: {Role}");
            }

            // Architecture section (always include for technical roles)
            if (string.IsNullOrEmpty(Role) ||
                Role.Equals("Backend", StringComparison.OrdinalIgnoreCase) ||
                Role.Equals("Developer", StringComparison.OrdinalIgnoreCase) ||
                Role.Equals("Plugin", StringComparison.OrdinalIgnoreCase))
            {
                sb.AppendLine("## Architecture");

                if (manifest.TryGetValue("architecture", out var archObj))
                {
                    var arch = archObj as Dictionary<object, object>;

                    if (arch != null && arch.TryGetValue("overview", out var overview))
                    {
                        sb.AppendLine(overview.ToString());
                    }

                    // Add module information
                    if (arch != null && arch.TryGetValue("modules", out var modulesObj))
                    {
                        var modules = modulesObj as Dictionary<object, object>;
                        if (modules != null)
                        {
                            sb.AppendLine("### Key Modules");

                            foreach (var moduleKV in modules)
                            {
                                var moduleName = moduleKV.Key.ToString();
                                var moduleData = moduleKV.Value as Dictionary<object, object>;

                                sb.AppendLine($"#### {moduleName}");

                                if (moduleData != null)
                                {
                                    if (moduleData.TryGetValue("description", out var moduleDesc))
                                    {
                                        sb.AppendLine(moduleDesc.ToString());
                                    }

                                    if (moduleData.TryGetValue("path", out var modulePath))
                                    {
                                        sb.AppendLine($"- Path: `{modulePath}`");
                                    }

                                    if (moduleData.TryGetValue("responsibilities", out var respObj))
                                    {
                                        var responsibilities = respObj as List<object>;
                                        if (responsibilities?.Count > 0)
                                        {
                                            sb.AppendLine("- Responsibilities:");
                                            foreach (var resp in responsibilities)
                                            {
                                                sb.AppendLine($"  - {resp}");
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }

                    // If Backend role, include plugin system details
                    if (string.IsNullOrEmpty(Role) ||
                        Role.Equals("Backend", StringComparison.OrdinalIgnoreCase) ||
                        Role.Equals("Plugin", StringComparison.OrdinalIgnoreCase))
                    {
                        if (arch != null && arch.TryGetValue("pluginSystem", out var pluginSysObj))
                        {
                            var pluginSystem = pluginSysObj as Dictionary<object, object>;
                            if (pluginSystem != null)
                            {
                                sb.AppendLine("### Plugin System");

                                if (pluginSystem.TryGetValue("description", out var psDesc))
                                {
                                    sb.AppendLine(psDesc.ToString());
                                }

                                if (pluginSystem.TryGetValue("pluginLifecycle", out var lifecycleObj))
                                {
                                    var lifecycle = lifecycleObj as List<object>;
                                    if (lifecycle?.Count > 0)
                                    {
                                        sb.AppendLine("**Plugin Lifecycle:**");
                                        foreach (var stage in lifecycle)
                                        {
                                            sb.AppendLine($"- {stage}");
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            // Directory Structure - relevant for all roles
            if (manifest.TryGetValue("directoryStructure", out var dirStructObj))
            {
                sb.AppendLine("## Directory Structure");
                var dirStruct = dirStructObj as Dictionary<object, object>;

                if (dirStruct != null)
                {
                    // Root directories
                    if (dirStruct.TryGetValue("root", out var rootDirsObj))
                    {
                        var rootDirs = rootDirsObj as Dictionary<object, object>;
                        if (rootDirs != null)
                        {
                            sb.AppendLine("### Root Directories");
                            foreach (var dirKV in rootDirs)
                            {
                                sb.AppendLine($"- `{dirKV.Key}`: {dirKV.Value}");
                            }
                        }
                    }

                    // Source structure - more relevant for developers
                    if (string.IsNullOrEmpty(Role) ||
                        !Role.Equals("Docs", StringComparison.OrdinalIgnoreCase))
                    {
                        if (dirStruct.TryGetValue("srcStructure", out var srcStructObj))
                        {
                            var srcStruct = srcStructObj as Dictionary<object, object>;
                            if (srcStruct != null)
                            {
                                sb.AppendLine("### Source Structure");
                                foreach (var dirKV in srcStruct)
                                {
                                    sb.AppendLine($"- `{dirKV.Key}`: {dirKV.Value}");
                                }
                            }
                        }
                    }
                }
            }

            // Naming Conventions
            if (manifest.TryGetValue("namingConventions", out var namingObj))
            {
                sb.AppendLine("## Naming Conventions");
                var naming = namingObj as Dictionary<object, object>;

                if (naming != null)
                {
                    foreach (var conventionType in naming)
                    {
                        var typeName = conventionType.Key.ToString();
                        var conventions = conventionType.Value as List<object>;

                        if (conventions?.Count > 0)
                        {
                            // Filter conventions based on role
                            if (string.IsNullOrEmpty(Role) ||
                                typeName.Equals("general", StringComparison.OrdinalIgnoreCase) ||
                                (Role.Equals("Docs", StringComparison.OrdinalIgnoreCase) && typeName.Equals("stories", StringComparison.OrdinalIgnoreCase)) ||
                                (Role.Equals("Plugin", StringComparison.OrdinalIgnoreCase) && typeName.Equals("plugins", StringComparison.OrdinalIgnoreCase)))
                            {
                                sb.AppendLine($"### {typeName}");
                                foreach (var convention in conventions)
                                {
                                    sb.AppendLine($"- {convention}");
                                }
                            }
                        }
                    }
                }
            }

            // Roadmap - include for all roles
            if (manifest.TryGetValue("roadmap", out var roadmapObj))
            {
                sb.AppendLine("## Roadmap");
                var roadmap = roadmapObj as Dictionary<object, object>;

                if (roadmap != null)
                {
                    if (roadmap.TryGetValue("current", out var currentObj))
                    {
                        var current = currentObj as List<object>;
                        if (current?.Count > 0)
                        {
                            sb.AppendLine("### Current Milestones");
                            foreach (var item in current)
                            {
                                sb.AppendLine($"- {item}");
                            }
                        }
                    }

                    if (roadmap.TryGetValue("upcoming", out var upcomingObj))
                    {
                        var upcoming = upcomingObj as List<object>;
                        if (upcoming?.Count > 0)
                        {
                            sb.AppendLine("### Upcoming");
                            foreach (var item in upcoming)
                            {
                                sb.AppendLine($"- {item}");
                            }
                        }
                    }
                }
            }

            // Glossary - include for all roles
            if (manifest.TryGetValue("glossary", out var glossaryObj))
            {
                sb.AppendLine("## Glossary");
                var glossary = glossaryObj as Dictionary<object, object>;

                if (glossary != null)
                {
                    foreach (var termKV in glossary)
                    {
                        sb.AppendLine($"- **{termKV.Key}**: {termKV.Value}");
                    }
                }
            }

            // FAQ - include for all roles
            if (manifest.TryGetValue("faq", out var faqObj))
            {
                sb.AppendLine("## FAQ");
                var faqItems = faqObj as List<object>;

                if (faqItems != null)
                {
                    foreach (var faqItemObj in faqItems)
                    {
                        var faqItem = faqItemObj as Dictionary<object, object>;
                        if (faqItem != null &&
                            faqItem.TryGetValue("question", out var question) &&
                            faqItem.TryGetValue("answer", out var answer))
                        {
                            sb.AppendLine($"### Q: {question}");
                            sb.AppendLine(answer.ToString());
                        }
                    }
                }
            }

            // Team Culture
            if (string.IsNullOrEmpty(Role) || !Role.Equals("Plugin", StringComparison.OrdinalIgnoreCase))
            {
                if (manifest.TryGetValue("teamCulture", out var cultureObj))
                {
                    sb.AppendLine("## Team Culture");
                    var culture = cultureObj as Dictionary<object, object>;

                    if (culture != null)
                    {
                        foreach (var cultureKV in culture)
                        {
                            var cultureName = cultureKV.Key.ToString();
                            var cultureItems = cultureKV.Value as List<object>;

                            if (cultureItems?.Count > 0)
                            {
                                sb.AppendLine($"### {cultureName}");
                                foreach (var item in cultureItems)
                                {
                                    sb.AppendLine($"- {item}");
                                }
                            }
                        }
                    }
                }
            }

            // Append progress report
            if (!string.IsNullOrEmpty(progressReport))
            {
                sb.AppendLine("## Project Progress");
                sb.AppendLine("```");
                sb.AppendLine(progressReport.Replace("```", "").Trim());
            }

            // Output context
            LogNormal("");
            LogHighlight("→ AI Context Generated Successfully!");
            LogNormal(sb.ToString());
            LogNormal("");
            LogSuccess("AI Context prepared successfully! You can now paste this into your GitHub Copilot Chat to bootstrap project context.");
            LogNormal("");
            LogHighlight("→ Tip: Use /sync, /roadmap, or /why commands in Copilot Chat to reference specific sections of the manifest.");
            LogNormal("");
        });

    Target SyncAIManifest => _ => _
        .Description("Updates the AI manifest by scanning the codebase")
        .Executes(() =>
        {
            LogHeader("🔄 UPDATING AI CONTEXT MANIFEST");
            LogNormal("Scanning codebase to update AI manifest...");

            // Check if manifest exists
            if (!File.Exists(AiManifestFile))
            {
                LogWarning("AI manifest file not found. Will create a new one at: " + AiManifestFile);
            }

            try
            {
                // Create ManifestSync instance and update manifest
                var manifestSync = new Modulus.Build.ManifestSync(
                    rootDirectory: RootDirectory.ToString(),
                    manifestPath: AiManifestFile.ToString(),
                    verbose: true);

                // Update sections
                manifestSync.UpdateDirectoryStructure();
                manifestSync.UpdateProjectInfo();
                manifestSync.UpdateNamingConventions();
                manifestSync.UpdateGlossary();

                // Save changes
                manifestSync.SaveManifest();

                LogSuccess("AI manifest successfully updated!");
                LogNormal("");
                LogHighlight("→ Run 'nuke StartAI' to see the updated context");
                LogNormal("");
            }
            catch (Exception ex)
            {
                LogError($"Failed to update AI manifest: {ex.Message}");
                LogError(ex.StackTrace);
                throw;
            }
        });

    // Helper method to determine if a section should be included based on role
    private bool ShouldIncludeSection(string section, string role)
    {
        if (string.IsNullOrEmpty(role))
            return true;

        // Role-based filtering logic
        switch (role.ToLower())
        {
            case "backend":
                // Backend developers need architecture, directory structure, naming conventions
                return true;
            case "frontend":
                // Frontend developers may not need some backend-specific sections
                return section != "pluginSystem" || section == "directoryStructure" || section == "namingConventions";
            case "plugin":
                // Plugin developers need plugin system info
                return section == "pluginSystem" || section == "directoryStructure" || section == "namingConventions" || section == "architecture";
            case "docs":
                // Documentation folks need structure and naming conventions
                return section == "directoryStructure" || section == "namingConventions" || section == "roadmap" || section == "glossary";
            default:
                return true;
        }
    }

    // Helper to filter modules based on role
    private bool ShouldIncludeModule(Dictionary<object, object> module, string role)
    {
        if (string.IsNullOrEmpty(role))
            return true;

        // Check if module has a path property
        if (!module.ContainsKey("path"))
            return true;

        string path = module["path"].ToString().ToLower();

        switch (role.ToLower())
        {
            case "frontend":
                return path.Contains("ui") || path.Contains("app") || path.Contains("desktop");
            case "backend":
                return !path.Contains("ui") || path.Contains("core") || path.Contains("plugins");
            case "plugin":
                return path.Contains("plugin") || path.Contains("sdk");
            default:
                return true;
        }
    }
}
