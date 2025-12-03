using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Modulus.Core.Runtime;

/// <summary>
/// Discovers modules in a specific physical directory.
/// </summary>
public class DirectoryModuleProvider : IModuleProvider
{
    private readonly string _rootPath;
    private readonly ILogger _logger;

    private readonly bool _isSystem;

    public DirectoryModuleProvider(string rootPath, ILogger logger, bool isSystem = false)
    {
        _rootPath = rootPath;
        _logger = logger;
        _isSystem = isSystem;
    }

    public bool IsSystemSource => _isSystem;

    public Task<IEnumerable<string>> GetModulePackagesAsync(CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(_rootPath))
        {
            _logger.LogDebug("Module directory {Path} does not exist. Skipping.", _rootPath);
            return Task.FromResult(Enumerable.Empty<string>());
        }

        var modulePaths = new List<string>();
        try
        {
            // Assume each subdirectory is a potential module
            foreach (var dir in Directory.GetDirectories(_rootPath))
            {
                if (File.Exists(Path.Combine(dir, "manifest.json")))
                {
                    modulePaths.Add(dir);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scanning directory {Path} for modules.", _rootPath);
        }

        return Task.FromResult((IEnumerable<string>)modulePaths);
    }
}


