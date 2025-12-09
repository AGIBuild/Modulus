using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Modulus.Architecture;

namespace Modulus.Core.Architecture;

/// <summary>
/// Interface for shared assembly catalog with diagnostics support.
/// </summary>
public interface ISharedAssemblyCatalog
{
    /// <summary>
    /// Gets all shared assembly names.
    /// </summary>
    IReadOnlyCollection<string> Names { get; }
    
    /// <summary>
    /// Checks if an assembly should be resolved from the shared context.
    /// </summary>
    bool IsShared(AssemblyName assemblyName);
    
    /// <summary>
    /// Gets all shared assembly entries with their sources.
    /// </summary>
    IReadOnlyCollection<SharedAssemblyEntry> GetEntries();
    
    /// <summary>
    /// Gets all recorded mismatches.
    /// </summary>
    IReadOnlyCollection<SharedAssemblyMismatch> GetMismatches();
    
    /// <summary>
    /// Adds manifest hints for a specific module load.
    /// Returns any mismatches found during hint processing.
    /// </summary>
    IReadOnlyList<SharedAssemblyMismatch> AddManifestHints(string moduleId, IEnumerable<string> hints);
}

/// <summary>
/// Catalog of assemblies that must be resolved from the shared domain.
/// Built from assembly-domain metadata, host configuration, and module manifest hints.
/// </summary>
public sealed class SharedAssemblyCatalog : ISharedAssemblyCatalog
{
    private readonly Dictionary<string, SharedAssemblyEntry> _entries;
    private readonly Dictionary<string, AssemblyDomainType> _domainMap;
    private readonly List<SharedAssemblyMismatch> _mismatches;
    private readonly ILogger<SharedAssemblyCatalog>? _logger;
    private readonly object _lock = new();

    private SharedAssemblyCatalog(
        Dictionary<string, SharedAssemblyEntry> entries,
        Dictionary<string, AssemblyDomainType> domainMap,
        List<SharedAssemblyMismatch> mismatches,
        ILogger<SharedAssemblyCatalog>? logger)
    {
        _entries = entries;
        _domainMap = domainMap;
        _mismatches = mismatches;
        _logger = logger;
    }

    /// <summary>
    /// Creates a SharedAssemblyCatalog from loaded assemblies and optional configuration.
    /// </summary>
    public static SharedAssemblyCatalog FromAssemblies(
        IEnumerable<Assembly> assemblies,
        IEnumerable<string>? configuredAssemblies = null,
        ILogger<SharedAssemblyCatalog>? logger = null)
    {
        var entries = new Dictionary<string, SharedAssemblyEntry>(StringComparer.OrdinalIgnoreCase);
        var domainMap = new Dictionary<string, AssemblyDomainType>(StringComparer.OrdinalIgnoreCase);
        var mismatches = new List<SharedAssemblyMismatch>();

        // Phase 1: Scan loaded assemblies for domain metadata
        foreach (var assembly in assemblies)
        {
            var name = assembly.GetName().Name;
            if (string.IsNullOrEmpty(name)) continue;

            var domain = AssemblyDomainInfo.GetDomainType(assembly);
            domainMap[name] = domain;

            if (domain == AssemblyDomainType.Shared)
            {
                entries[name] = new SharedAssemblyEntry
                {
                    Name = name,
                    Source = SharedAssemblySource.DomainAttribute,
                    DeclaredDomain = domain
                };
            }
        }

        // Phase 2: Merge host configuration entries
        if (configuredAssemblies != null)
        {
            foreach (var configName in configuredAssemblies)
            {
                if (string.IsNullOrWhiteSpace(configName))
                {
                    logger?.LogWarning("Empty assembly name in host configuration, skipping.");
                    mismatches.Add(new SharedAssemblyMismatch
                    {
                        AssemblyName = "(empty)",
                        RequestSource = SharedAssemblySource.HostConfig,
                        Reason = "Empty or whitespace assembly name in configuration"
                    });
                    continue;
                }

                var trimmed = configName.Trim();
                if (trimmed.Length > SharedAssemblyOptions.MaxAssemblyNameLength)
                {
                    logger?.LogWarning("Assembly name '{Name}' exceeds max length, skipping.", trimmed[..50]);
                    mismatches.Add(new SharedAssemblyMismatch
                    {
                        AssemblyName = trimmed[..Math.Min(50, trimmed.Length)],
                        RequestSource = SharedAssemblySource.HostConfig,
                        Reason = $"Assembly name exceeds maximum length of {SharedAssemblyOptions.MaxAssemblyNameLength}"
                    });
                    continue;
                }

                // Skip if already added from domain metadata
                if (entries.ContainsKey(trimmed))
                {
                    logger?.LogDebug("Assembly '{Name}' already in shared catalog from domain metadata.", trimmed);
                    continue;
                }

                // Check if it conflicts with Module domain
                if (domainMap.TryGetValue(trimmed, out var declaredDomain) && declaredDomain == AssemblyDomainType.Module)
                {
                    logger?.LogWarning(
                        "Assembly '{Name}' is declared Module-domain but added as shared via host config. Adding with mismatch diagnostic.",
                        trimmed);
                    
                    entries[trimmed] = new SharedAssemblyEntry
                    {
                        Name = trimmed,
                        Source = SharedAssemblySource.HostConfig,
                        DeclaredDomain = declaredDomain,
                        HasMismatch = true,
                        MismatchReason = "Declared as Module-domain but configured as shared"
                    };
                    
                    mismatches.Add(new SharedAssemblyMismatch
                    {
                        AssemblyName = trimmed,
                        RequestSource = SharedAssemblySource.HostConfig,
                        DeclaredDomain = declaredDomain,
                        Reason = "Assembly is declared Module-domain but configured as shared via host configuration"
                    });
                }
                else
                {
                    entries[trimmed] = new SharedAssemblyEntry
                    {
                        Name = trimmed,
                        Source = SharedAssemblySource.HostConfig,
                        DeclaredDomain = domainMap.TryGetValue(trimmed, out var d) ? d : null
                    };
                }
            }
        }

        return new SharedAssemblyCatalog(entries, domainMap, mismatches, logger);
    }

    public IReadOnlyCollection<string> Names
    {
        get
        {
            lock (_lock)
            {
                return _entries.Keys.ToList();
            }
        }
    }

    public bool IsShared(AssemblyName assemblyName)
    {
        if (assemblyName.Name is null) return false;

        lock (_lock)
        {
            var isShared = _entries.ContainsKey(assemblyName.Name);
            
            if (isShared && _domainMap.TryGetValue(assemblyName.Name, out var domain) && domain == AssemblyDomainType.Module)
            {
                _logger?.LogWarning("Assembly {Assembly} is marked shared but declared as Module.", assemblyName.Name);
            }

            return isShared;
        }
    }

    public IReadOnlyCollection<SharedAssemblyEntry> GetEntries()
    {
        lock (_lock)
        {
            return _entries.Values.ToList();
        }
    }

    public IReadOnlyCollection<SharedAssemblyMismatch> GetMismatches()
    {
        lock (_lock)
        {
            return _mismatches.ToList();
        }
    }

    public IReadOnlyList<SharedAssemblyMismatch> AddManifestHints(string moduleId, IEnumerable<string> hints)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(moduleId);
        ArgumentNullException.ThrowIfNull(hints);

        var newMismatches = new List<SharedAssemblyMismatch>();
        var hintList = hints.ToList();
        
        if (hintList.Count > SharedAssemblyOptions.MaxManifestHints)
        {
            _logger?.LogWarning(
                "Module {ModuleId} provided {Count} shared assembly hints, exceeding max of {Max}. Truncating.",
                moduleId, hintList.Count, SharedAssemblyOptions.MaxManifestHints);
            
            newMismatches.Add(new SharedAssemblyMismatch
            {
                AssemblyName = "(manifest)",
                ModuleId = moduleId,
                RequestSource = SharedAssemblySource.ManifestHint,
                Reason = $"Manifest provided {hintList.Count} hints, exceeding maximum of {SharedAssemblyOptions.MaxManifestHints}"
            });
            
            hintList = hintList.Take(SharedAssemblyOptions.MaxManifestHints).ToList();
        }

        lock (_lock)
        {
            foreach (var hint in hintList)
            {
                if (string.IsNullOrWhiteSpace(hint))
                {
                    _logger?.LogWarning("Module {ModuleId} provided empty shared assembly hint.", moduleId);
                    var mismatch = new SharedAssemblyMismatch
                    {
                        AssemblyName = "(empty)",
                        ModuleId = moduleId,
                        RequestSource = SharedAssemblySource.ManifestHint,
                        Reason = "Empty or whitespace assembly name in manifest hints"
                    };
                    newMismatches.Add(mismatch);
                    _mismatches.Add(mismatch);
                    continue;
                }

                var trimmed = hint.Trim();
                if (trimmed.Length > SharedAssemblyOptions.MaxAssemblyNameLength)
                {
                    _logger?.LogWarning("Module {ModuleId} provided hint '{Name}' exceeding max length.", moduleId, trimmed[..50]);
                    var mismatch = new SharedAssemblyMismatch
                    {
                        AssemblyName = trimmed[..Math.Min(50, trimmed.Length)],
                        ModuleId = moduleId,
                        RequestSource = SharedAssemblySource.ManifestHint,
                        Reason = $"Assembly name exceeds maximum length of {SharedAssemblyOptions.MaxAssemblyNameLength}"
                    };
                    newMismatches.Add(mismatch);
                    _mismatches.Add(mismatch);
                    continue;
                }

                // Skip if already in catalog
                if (_entries.ContainsKey(trimmed))
                {
                    _logger?.LogDebug("Module {ModuleId} hint '{Name}' already in shared catalog.", moduleId, trimmed);
                    continue;
                }

                // Check for domain conflict
                if (_domainMap.TryGetValue(trimmed, out var declaredDomain) && declaredDomain == AssemblyDomainType.Module)
                {
                    _logger?.LogWarning(
                        "Module {ModuleId} requested '{Name}' as shared but it is declared Module-domain.",
                        moduleId, trimmed);
                    
                    var mismatch = new SharedAssemblyMismatch
                    {
                        AssemblyName = trimmed,
                        ModuleId = moduleId,
                        RequestSource = SharedAssemblySource.ManifestHint,
                        DeclaredDomain = declaredDomain,
                        Reason = "Assembly is declared Module-domain but requested as shared via manifest hint"
                    };
                    newMismatches.Add(mismatch);
                    _mismatches.Add(mismatch);
                    
                    // Still add it (per design: hints are honored but diagnostics surface misuse)
                    _entries[trimmed] = new SharedAssemblyEntry
                    {
                        Name = trimmed,
                        Source = SharedAssemblySource.ManifestHint,
                        DeclaredDomain = declaredDomain,
                        SourceModuleId = moduleId,
                        HasMismatch = true,
                        MismatchReason = "Declared as Module-domain but requested as shared"
                    };
                }
                else
                {
                    _entries[trimmed] = new SharedAssemblyEntry
                    {
                        Name = trimmed,
                        Source = SharedAssemblySource.ManifestHint,
                        DeclaredDomain = _domainMap.TryGetValue(trimmed, out var d) ? d : null,
                        SourceModuleId = moduleId
                    };
                }
            }
        }

        return newMismatches;
    }
}
