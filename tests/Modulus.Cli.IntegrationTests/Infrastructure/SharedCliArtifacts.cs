using System.Collections.Concurrent;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;

namespace Modulus.Cli.IntegrationTests.Infrastructure;

/// <summary>
/// Caches expensive CLI-generated artifacts across the whole test run to reduce end-to-end time.
/// Generates modules via CLI once (new + pack) and reuses:
/// - .modpkg path for Install/List/Uninstall tests
/// - extracted directory for Install-from-directory tests
/// </summary>
internal static class SharedCliArtifacts
{
    private static readonly SemaphoreSlim Gate = new(1, 1);
    private static readonly ConcurrentDictionary<string, SharedModuleArtifact> Cache = new(StringComparer.OrdinalIgnoreCase);

    private static CliTestContext? _context;
    private static CliRunner? _runner;

    public static Task<SharedModuleArtifact> GetAvaloniaAsync() => GetAsync("SharedAvalonia", "module-avalonia");
    public static Task<SharedModuleArtifact> GetAvaloniaAAsync() => GetAsync("SharedModuleA", "module-avalonia");
    public static Task<SharedModuleArtifact> GetAvaloniaBAsync() => GetAsync("SharedModuleB", "module-avalonia");

    public static async Task<SharedModuleArtifact> GetAsync(string moduleName, string template)
    {
        var key = $"{template}:{moduleName}";
        if (Cache.TryGetValue(key, out var existing))
            return existing;

        await Gate.WaitAsync();
        try
        {
            if (Cache.TryGetValue(key, out existing))
                return existing;

            _context ??= new CliTestContext();
            _runner ??= new CliRunner(_context);

            // Ensure output isolation per artifact to avoid accidental cross-pickup.
            var artifactOutputDir = Path.Combine(_context.OutputDirectory, $"{moduleName}-{template}");
            Directory.CreateDirectory(artifactOutputDir);

            // Generate (or overwrite) module in the shared context.
            var newResult = await _runner.NewAsync(moduleName, template: template, outputPath: _context.WorkingDirectory, force: true);
            if (!newResult.IsSuccess)
                throw new InvalidOperationException($"[SharedCliArtifacts:new] Failed: {newResult.CombinedOutput}");

            var moduleDir = Path.Combine(_context.WorkingDirectory, moduleName);

            // Pack (builds if needed). This is the expensive step we want to do once per moduleName+target.
            var packResult = await _runner.PackAsync(path: moduleDir, output: artifactOutputDir, configuration: "Release");
            if (!packResult.IsSuccess)
                throw new InvalidOperationException($"[SharedCliArtifacts:pack] Failed: {packResult.CombinedOutput}");

            var packages = Directory.GetFiles(artifactOutputDir, "*.modpkg");
            if (packages.Length == 0)
                throw new InvalidOperationException($"[SharedCliArtifacts:pack] No .modpkg produced. Output: {packResult.CombinedOutput}");

            // Pick newest in case previous runs left files behind.
            var packagePath = packages
                .Select(p => new FileInfo(p))
                .OrderByDescending(f => f.LastWriteTimeUtc)
                .First()
                .FullName;

            var extractedDir = Path.Combine(artifactOutputDir, "extracted");
            if (Directory.Exists(extractedDir))
            {
                try { Directory.Delete(extractedDir, recursive: true); } catch { /* ignore */ }
            }
            Directory.CreateDirectory(extractedDir);
            ZipFile.ExtractToDirectory(packagePath, extractedDir);

            var artifact = new SharedModuleArtifact(moduleName, template, moduleDir, packagePath, extractedDir);
            Cache[key] = artifact;
            return artifact;
        }
        finally
        {
            Gate.Release();
        }
    }
}

internal sealed record SharedModuleArtifact(
    string ModuleName,
    string Template,
    string ModuleDir,
    string PackagePath,
    string ExtractedDir);


