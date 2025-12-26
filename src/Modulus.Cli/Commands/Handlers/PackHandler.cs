using System.IO.Compression;
using System.Text.Json;
using System.Xml.Linq;
using Modulus.Cli.Services;
using Modulus.Core.Architecture;
using Modulus.Sdk;

namespace Modulus.Cli.Commands.Handlers;

/// <summary>
/// Handles module packaging for <c>modulus pack</c>.
/// </summary>
public sealed class PackHandler
{
    private static readonly HashSet<string> BuiltInSharedAssemblies =
        new(SharedAssemblyPolicy.GetBuiltInSharedAssemblies(), StringComparer.OrdinalIgnoreCase);

    private static readonly string[] FrameworkAssemblyPrefixes =
    [
        "System.",
        "Microsoft.Extensions.",
        "Microsoft.EntityFrameworkCore.",
        "Microsoft.AspNetCore.",
        "Microsoft.CSharp",
        "mscorlib",
        "netstandard",
    ];

    private readonly ICliConsole _console;
    private readonly IProcessRunner _processRunner;

    public PackHandler(ICliConsole console, IProcessRunner processRunner)
    {
        _console = console;
        _processRunner = processRunner;
    }

    public async Task<int> ExecuteAsync(
        string? path,
        string? output,
        string configuration,
        bool noBuild,
        bool verbose,
        CancellationToken cancellationToken)
    {
        var projectDir = path != null
            ? Path.GetFullPath(path)
            : Directory.GetCurrentDirectory();

        if (!Directory.Exists(projectDir))
        {
            await _console.Error.WriteLineAsync($"Error: Directory not found: {projectDir}");
            return 1;
        }

        var moduleName = Path.GetFileName(projectDir);

        var outputDir = output != null
            ? Path.GetFullPath(output)
            : Path.Combine(projectDir, "output");

        await _console.Out.WriteLineAsync($"Packaging module: {moduleName}");
        if (verbose)
        {
            await _console.Out.WriteLineAsync($"  Project: {projectDir}");
            await _console.Out.WriteLineAsync($"  Output: {outputDir}");
        }

        var (projectFile, _) = ProjectLocator.FindProjectFile(projectDir);
        if (projectFile == null)
        {
            await _console.Error.WriteLineAsync("Error: No module project found.");
            await _console.Error.WriteLineAsync("Expected: .sln file or .csproj file with Modulus.Sdk reference");
            return 1;
        }

        if (!noBuild)
        {
            await _console.Out.WriteLineAsync();
            await _console.Out.WriteLineAsync("Step 1: Building project...");

            var buildOk = await RunDotnetBuildAsync(projectFile, projectDir, configuration, verbose, cancellationToken);
            if (!buildOk)
            {
                await _console.Error.WriteLineAsync("Error: Build failed. Fix errors and try again, or use --no-build to skip.");
                return 1;
            }

            await _console.Out.WriteLineAsync("  ✓ Build succeeded");
        }
        else
        {
            await _console.Out.WriteLineAsync("  Skipping build (--no-build)");
        }

        await _console.Out.WriteLineAsync();
        await _console.Out.WriteLineAsync("Step 2: Collecting files...");

        var buildOutputDir = FindBuildOutput(projectDir, configuration);
        if (buildOutputDir == null)
        {
            await _console.Error.WriteLineAsync("Error: Could not find build output directory.");
            await _console.Error.WriteLineAsync($"  Expected: bin/{configuration}/ or similar");
            return 1;
        }

        if (verbose)
        {
            await _console.Out.WriteLineAsync($"  Build output: {buildOutputDir}");
        }

        var manifestPath = FindManifest(projectDir, buildOutputDir);
        if (manifestPath == null)
        {
            await _console.Error.WriteLineAsync("Error: extension.vsixmanifest not found.");
            await _console.Error.WriteLineAsync("  Create a manifest file or use 'modulus new' to create a new module.");
            return 1;
        }

        var (moduleId, version, displayName) = ReadManifestInfo(manifestPath);
        if (moduleId == null)
        {
            await _console.Error.WriteLineAsync("Error: Could not read module identity from manifest.");
            return 1;
        }

        var packageBaseName = SanitizeFileName(displayName ?? moduleName);

        await _console.Out.WriteLineAsync($"  Module: {displayName ?? moduleName}");
        await _console.Out.WriteLineAsync($"  ID: {moduleId}");
        await _console.Out.WriteLineAsync($"  Version: {version}");

        var installationTargets = ReadInstallationTargetHostIds(manifestPath);
        var hostConfigSharedAssemblies = TryLoadHostSharedAssembliesFromRepo(projectDir, installationTargets);
        var hostConfigSharedPrefixes = TryLoadHostSharedAssemblyPrefixesFromRepo(projectDir, installationTargets);

        var presetPrefixes = installationTargets.SelectMany(SharedAssemblyPolicy.GetBuiltInPrefixPresetsForHost).ToList();
        var sharedAssemblyPrefixes = new HashSet<string>(
            SharedAssemblyPolicy.MergeWithConfiguredPrefixes(hostConfigSharedPrefixes, presetPrefixes),
            StringComparer.OrdinalIgnoreCase);

        var sharedAssemblyNames = new HashSet<string>(
            SharedAssemblyPolicy.MergeWithConfiguredAssemblies(hostConfigSharedAssemblies),
            StringComparer.OrdinalIgnoreCase);

        var filesToPack = CollectFilesForPackaging(
            projectDir,
            buildOutputDir,
            manifestPath,
            sharedAssemblyNames,
            sharedAssemblyPrefixes,
            hostConfigSharedAssemblies,
            hostConfigSharedPrefixes,
            verbose);

        await _console.Out.WriteLineAsync($"  Files collected: {filesToPack.Count}");

        await _console.Out.WriteLineAsync();
        await _console.Out.WriteLineAsync("Step 3: Creating package...");

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
                    await _console.Out.WriteLineAsync($"    Adding: {entryName}");
                }
                archive.CreateEntryFromFile(sourcePath, entryName, CompressionLevel.Optimal);
            }
        }

        var fileInfo = new FileInfo(packagePath);
        await _console.Out.WriteLineAsync($"  ✓ Package created: {packageName}");
        await _console.Out.WriteLineAsync($"    Size: {FormatFileSize(fileInfo.Length)}");

        await _console.Out.WriteLineAsync();
        await _console.Out.WriteLineAsync("✓ Packaging complete!");
        await _console.Out.WriteLineAsync($"  Output: {packagePath}");
        await _console.Out.WriteLineAsync();
        await _console.Out.WriteLineAsync("To install this module:");
        await _console.Out.WriteLineAsync($"  modulus install \"{packagePath}\"");
        await _console.Out.WriteLineAsync();

        return 0;
    }

    private async Task<bool> RunDotnetBuildAsync(
        string projectFile,
        string workingDir,
        string configuration,
        bool verbose,
        CancellationToken cancellationToken)
    {
        var result = await _processRunner.RunAsync(
            new ProcessRunRequest(
                "dotnet",
                $"build \"{projectFile}\" --configuration {configuration}",
                workingDir,
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

        return result.ExitCode == 0;
    }

    private static string? FindBuildOutput(string projectDir, string configuration)
    {
        var coreProjectDir = Directory.GetDirectories(projectDir)
            .FirstOrDefault(d => Path.GetFileName(d).EndsWith(".Core", StringComparison.OrdinalIgnoreCase));

        if (coreProjectDir != null)
        {
            var coreBinDir = Path.Combine(coreProjectDir, "bin", configuration);
            if (Directory.Exists(coreBinDir))
            {
                var tfmDirs = Directory.GetDirectories(coreBinDir);
                if (tfmDirs.Length > 0)
                {
                    return tfmDirs[0];
                }
                return coreBinDir;
            }
        }

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

        return ProjectLocator.FindOutputDirectory(projectDir, configuration);
    }

    private static string? FindManifest(string projectDir, string buildOutputDir)
    {
        var manifestInOutput = Path.Combine(buildOutputDir, "extension.vsixmanifest");
        if (File.Exists(manifestInOutput))
        {
            return manifestInOutput;
        }

        var manifestInRoot = Path.Combine(projectDir, "extension.vsixmanifest");
        if (File.Exists(manifestInRoot))
        {
            return manifestInRoot;
        }

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

    private List<(string SourcePath, string EntryName)> CollectFilesForPackaging(
        string projectDir,
        string buildOutputDir,
        string manifestPath,
        HashSet<string> sharedAssemblyNames,
        HashSet<string> sharedAssemblyPrefixes,
        IReadOnlyCollection<string>? hostConfigSharedAssemblies,
        IReadOnlyCollection<string>? hostConfigSharedPrefixes,
        bool verbose)
    {
        var files = new List<(string, string)>();
        var addedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var skippedShared = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        files.Add((manifestPath, "extension.vsixmanifest"));
        addedFiles.Add("extension.vsixmanifest");

        var allOutputDirs = new List<string> { buildOutputDir };

        foreach (var subDir in Directory.GetDirectories(projectDir))
        {
            var subDirName = Path.GetFileName(subDir);
            if (subDirName.Contains(".UI.") || subDirName.EndsWith(".Core", StringComparison.OrdinalIgnoreCase))
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

        foreach (var dir in allOutputDirs.Distinct())
        {
            foreach (var dll in Directory.GetFiles(dir, "*.dll"))
            {
                var fileName = Path.GetFileName(dll);

                if (IsSharedAssembly(fileName, sharedAssemblyNames, sharedAssemblyPrefixes, out var reason))
                {
                    skippedShared[fileName] = reason;
                    continue;
                }

                if (!addedFiles.Contains(fileName))
                {
                    files.Add((dll, fileName));
                    addedFiles.Add(fileName);
                }
            }
        }

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

        if (verbose && skippedShared.Count > 0)
        {
            _console.Out.WriteLine();
            _console.Out.WriteLine("  Shared assemblies excluded:");
            foreach (var name in skippedShared.Keys.OrderBy(s => s, StringComparer.OrdinalIgnoreCase))
            {
                var simpleName = Path.GetFileNameWithoutExtension(name);
                var source =
                    BuiltInSharedAssemblies.Contains(simpleName) ? "built-in" :
                    hostConfigSharedAssemblies?.Contains(simpleName, StringComparer.OrdinalIgnoreCase) == true ? "host-config" :
                    hostConfigSharedPrefixes?.Any(p => simpleName.StartsWith(p, StringComparison.OrdinalIgnoreCase)) == true ? "host-config-prefix" :
                    sharedAssemblyPrefixes.Any(p => simpleName.StartsWith(p, StringComparison.OrdinalIgnoreCase)) ? "host-prefix" :
                    "framework";
                _console.Out.WriteLine($"    - {name} ({source}; {skippedShared[name]})");
            }
        }

        return files;
    }

    private static bool IsSharedAssembly(
        string fileName,
        HashSet<string> sharedAssemblyNames,
        HashSet<string> sharedAssemblyPrefixes,
        out string reason)
    {
        var name = Path.GetFileNameWithoutExtension(fileName);

        if (sharedAssemblyNames.Contains(name))
        {
            reason = "exact name";
            return true;
        }

        foreach (var prefix in FrameworkAssemblyPrefixes)
        {
            if (name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                reason = $"framework prefix '{prefix}'";
                return true;
            }
        }

        foreach (var prefix in sharedAssemblyPrefixes)
        {
            if (name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                reason = $"shared prefix '{prefix}'";
                return true;
            }
        }

        reason = string.Empty;
        return false;
    }

    private static IReadOnlyList<string> ReadInstallationTargetHostIds(string manifestPath)
    {
        try
        {
            var doc = XDocument.Load(manifestPath);
            var ns = doc.Root?.GetDefaultNamespace() ?? XNamespace.None;

            return doc.Descendants(ns + "InstallationTarget")
                .Select(e => e.Attribute("Id")?.Value)
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .Select(v => v!.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
        catch
        {
            return Array.Empty<string>();
        }
    }

    private static IReadOnlyCollection<string>? TryLoadHostSharedAssembliesFromRepo(
        string projectDir,
        IReadOnlyList<string> installationTargetHostIds)
    {
        var repoRoot = TryFindRepoRoot(projectDir);
        if (repoRoot == null)
        {
            return null;
        }

        var shared = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var hostId in installationTargetHostIds)
        {
            var appsettingsPath = GetHostAppSettingsPath(repoRoot, hostId);
            if (appsettingsPath == null || !File.Exists(appsettingsPath))
            {
                continue;
            }

            try
            {
                using var json = JsonDocument.Parse(File.ReadAllText(appsettingsPath));
                if (json.RootElement.TryGetProperty("modulus", out var modulus) &&
                    modulus.TryGetProperty("sharedAssemblies", out var list) &&
                    list.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in list.EnumerateArray())
                    {
                        if (item.ValueKind == JsonValueKind.String)
                        {
                            var s = item.GetString();
                            if (!string.IsNullOrWhiteSpace(s))
                            {
                                shared.Add(s.Trim());
                            }
                        }
                    }
                }
            }
            catch
            {
                // ignore invalid host config
            }
        }

        return shared.Count == 0 ? null : shared;
    }

    private static IReadOnlyCollection<string>? TryLoadHostSharedAssemblyPrefixesFromRepo(
        string projectDir,
        IReadOnlyList<string> installationTargetHostIds)
    {
        var repoRoot = TryFindRepoRoot(projectDir);
        if (repoRoot == null)
        {
            return null;
        }

        var prefixes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var hostId in installationTargetHostIds)
        {
            var appsettingsPath = GetHostAppSettingsPath(repoRoot, hostId);
            if (appsettingsPath == null || !File.Exists(appsettingsPath))
            {
                continue;
            }

            try
            {
                using var json = JsonDocument.Parse(File.ReadAllText(appsettingsPath));
                if (json.RootElement.TryGetProperty("modulus", out var modulus) &&
                    modulus.TryGetProperty("sharedAssemblyPrefixes", out var list) &&
                    list.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in list.EnumerateArray())
                    {
                        if (item.ValueKind == JsonValueKind.String)
                        {
                            var s = item.GetString();
                            if (!string.IsNullOrWhiteSpace(s))
                            {
                                prefixes.Add(s.Trim());
                            }
                        }
                    }
                }
            }
            catch
            {
                // ignore invalid host config
            }
        }

        return prefixes.Count == 0 ? null : prefixes;
    }

    private static string? TryFindRepoRoot(string startDir)
    {
        var current = new DirectoryInfo(startDir);
        while (current != null)
        {
            if (File.Exists(Path.Combine(current.FullName, "Modulus.sln")))
            {
                return current.FullName;
            }
            current = current.Parent;
        }
        return null;
    }

    private static string? GetHostAppSettingsPath(string repoRoot, string hostId)
    {
        if (string.Equals(hostId, ModulusHostIds.Avalonia, StringComparison.OrdinalIgnoreCase))
        {
            return Path.Combine(repoRoot, "src", "Hosts", "Modulus.Host.Avalonia", "appsettings.json");
        }

        if (string.Equals(hostId, ModulusHostIds.Blazor, StringComparison.OrdinalIgnoreCase))
        {
            return Path.Combine(repoRoot, "src", "Hosts", "Modulus.Host.Blazor", "appsettings.json");
        }

        return null;
    }

    private static string SanitizeFileName(string name)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        return new string(name.Select(c => invalidChars.Contains(c) ? '_' : c).ToArray());
    }

    private static string FormatFileSize(long bytes)
    {
        if (bytes < 1024) return $"{bytes} B";
        if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
        if (bytes < 1024 * 1024 * 1024) return $"{bytes / (1024.0 * 1024.0):F1} MB";
        return $"{bytes / (1024.0 * 1024.0 * 1024.0):F1} GB";
    }
}


