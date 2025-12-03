using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Modulus.Core.Manifest;

namespace Modulus.Core.Runtime;

public class DevelopmentModuleScanningProvider : IModuleProvider
{
    private readonly string _solutionRoot;
    private readonly string _hostType;
    private readonly ILogger _logger;

    /// <summary>
    /// Creates a development module scanning provider.
    /// </summary>
    /// <param name="solutionRoot">Path to solution root directory.</param>
    /// <param name="hostType">Current host type (e.g., "BlazorApp", "AvaloniaApp") to filter UI projects.</param>
    /// <param name="logger">Logger instance.</param>
    public DevelopmentModuleScanningProvider(string solutionRoot, string hostType, ILogger logger)
    {
        _solutionRoot = solutionRoot;
        _hostType = hostType;
        _logger = logger;
    }

    public bool IsSystemSource => true;

    public Task<IEnumerable<string>> GetModulePackagesAsync(CancellationToken cancellationToken = default)
    {
        var paths = new List<string>();
        
        if (!Directory.Exists(_solutionRoot))
        {
            _logger.LogWarning("Solution root {Path} not found.", _solutionRoot);
            return Task.FromResult((IEnumerable<string>)paths);
        }

        var modulesDir = Path.Combine(_solutionRoot, "src", "Modules");
        if (!Directory.Exists(modulesDir))
        {
             return Task.FromResult((IEnumerable<string>)paths);
        }

        // Determine the UI project suffix based on host type
        var uiProjectSuffix = GetUiProjectSuffix(_hostType);

        try
        {
            // Scan src/Modules/*
            foreach (var moduleProjectDir in Directory.GetDirectories(modulesDir))
            {
                // Look for the UI project directory matching the current host
                // e.g., EchoPlugin.UI.Blazor for BlazorApp, EchoPlugin.UI.Avalonia for AvaloniaApp
                var uiProjectDirs = Directory.GetDirectories(moduleProjectDir, $"*{uiProjectSuffix}", SearchOption.TopDirectoryOnly);
                
                foreach (var uiProjectDir in uiProjectDirs)
                {
                    var binDir = Path.Combine(uiProjectDir, "bin");
                    if (!Directory.Exists(binDir)) continue;

                    // Look for Debug/Release output directories
                    var outputDirs = Directory.GetDirectories(binDir, "*", SearchOption.AllDirectories)
                        .Where(d => (d.Contains("Debug") || d.Contains("Release")) && 
                                    File.Exists(Path.Combine(d, "manifest.json")));

                    foreach (var outDir in outputDirs)
                    {
                        paths.Add(outDir);
                        _logger.LogDebug("Found module package at {Path} for host {HostType}", outDir, _hostType);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scanning for development modules.");
        }

        // De-duplicate by module ID (prefer first found, typically Debug)
        var uniquePaths = paths
            .GroupBy(p => GetModuleIdFromPath(p))
            .Select(g => g.First())
            .ToList();

        return Task.FromResult((IEnumerable<string>)uniquePaths);
    }

    private static string GetUiProjectSuffix(string hostType)
    {
        return hostType switch
        {
            HostType.Blazor => ".UI.Blazor",
            HostType.Avalonia => ".UI.Avalonia",
            _ => ".UI"
        };
    }

    private static string GetModuleIdFromPath(string path)
    {
        // Extract module ID from manifest.json if possible, or use directory name
        var manifestPath = Path.Combine(path, "manifest.json");
        if (File.Exists(manifestPath))
        {
            try
            {
                var manifest = ManifestReader.ReadFromFileAsync(manifestPath).GetAwaiter().GetResult();
                if (manifest != null) return manifest.Id;
            }
            catch { /* Ignore, fall back to directory name */ }
        }
        
        // Fall back to parent directory name
        return new DirectoryInfo(path).Parent?.Parent?.Parent?.Name ?? path;
    }
}

