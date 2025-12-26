using System.Collections.Generic;

namespace Modulus.Core.Architecture;

/// <summary>
/// Provides diagnostics information for shared assembly configuration and resolution.
/// </summary>
public sealed class SharedAssemblyDiagnostics
{
    /// <summary>
    /// All shared assembly entries with their sources.
    /// </summary>
    public required IReadOnlyCollection<SharedAssemblyEntry> Entries { get; init; }
    
    /// <summary>
    /// Entries added from domain metadata (highest priority).
    /// </summary>
    public required IReadOnlyCollection<SharedAssemblyEntry> DomainEntries { get; init; }
    
    /// <summary>
    /// Entries added from host configuration.
    /// </summary>
    public required IReadOnlyCollection<SharedAssemblyEntry> ConfigEntries { get; init; }
    
    /// <summary>
    /// Entries added from module manifest hints.
    /// </summary>
    public required IReadOnlyCollection<SharedAssemblyEntry> ManifestEntries { get; init; }
    
    /// <summary>
    /// All recorded mismatches (assemblies requested as shared but declared differently or missing).
    /// </summary>
    public required IReadOnlyCollection<SharedAssemblyMismatch> Mismatches { get; init; }

    /// <summary>
    /// Configured prefix rules for shared assemblies.
    /// </summary>
    public required IReadOnlyCollection<string> PrefixRules { get; init; }
    
    /// <summary>
    /// Number of entries with mismatch warnings.
    /// </summary>
    public int MismatchWarningCount { get; init; }
    
    /// <summary>
    /// Total count of shared assemblies.
    /// </summary>
    public int TotalCount => Entries.Count;
}

/// <summary>
/// Service interface for shared assembly diagnostics.
/// </summary>
public interface ISharedAssemblyDiagnosticsService
{
    /// <summary>
    /// Gets the current shared assembly diagnostics snapshot.
    /// </summary>
    SharedAssemblyDiagnostics GetDiagnostics();
    
    /// <summary>
    /// Gets entries by source.
    /// </summary>
    IReadOnlyCollection<SharedAssemblyEntry> GetEntriesBySource(SharedAssemblySource source);
    
    /// <summary>
    /// Gets entries for a specific module (manifest hints only).
    /// </summary>
    IReadOnlyCollection<SharedAssemblyEntry> GetEntriesForModule(string moduleId);
    
    /// <summary>
    /// Gets mismatches for a specific module.
    /// </summary>
    IReadOnlyCollection<SharedAssemblyMismatch> GetMismatchesForModule(string moduleId);
}

/// <summary>
/// Default implementation of shared assembly diagnostics service.
/// </summary>
public sealed class SharedAssemblyDiagnosticsService : ISharedAssemblyDiagnosticsService
{
    private readonly ISharedAssemblyCatalog _catalog;
    
    public SharedAssemblyDiagnosticsService(ISharedAssemblyCatalog catalog)
    {
        _catalog = catalog;
    }
    
    public SharedAssemblyDiagnostics GetDiagnostics()
    {
        var entries = _catalog.GetEntries();
        var mismatches = _catalog.GetMismatches();
        var prefixes = _catalog.GetPrefixRules();
        
        return new SharedAssemblyDiagnostics
        {
            Entries = entries,
            DomainEntries = entries.Where(e => e.Source == SharedAssemblySource.DomainAttribute).ToList(),
            ConfigEntries = entries.Where(e => e.Source == SharedAssemblySource.HostConfig).ToList(),
            ManifestEntries = entries.Where(e => e.Source == SharedAssemblySource.ManifestHint).ToList(),
            Mismatches = mismatches,
            PrefixRules = prefixes,
            MismatchWarningCount = entries.Count(e => e.HasMismatch)
        };
    }
    
    public IReadOnlyCollection<SharedAssemblyEntry> GetEntriesBySource(SharedAssemblySource source)
    {
        return _catalog.GetEntries().Where(e => e.Source == source).ToList();
    }
    
    public IReadOnlyCollection<SharedAssemblyEntry> GetEntriesForModule(string moduleId)
    {
        return _catalog.GetEntries()
            .Where(e => e.Source == SharedAssemblySource.ManifestHint && 
                        string.Equals(e.SourceModuleId, moduleId, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }
    
    public IReadOnlyCollection<SharedAssemblyMismatch> GetMismatchesForModule(string moduleId)
    {
        return _catalog.GetMismatches()
            .Where(m => string.Equals(m.ModuleId, moduleId, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }
}

