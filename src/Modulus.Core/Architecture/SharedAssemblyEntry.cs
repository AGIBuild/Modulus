using Modulus.Architecture;

namespace Modulus.Core.Architecture;

/// <summary>
/// Represents a shared assembly entry with its source and metadata.
/// </summary>
public sealed record SharedAssemblyEntry
{
    /// <summary>
    /// Simple assembly name (without extension or version).
    /// </summary>
    public required string Name { get; init; }
    
    /// <summary>
    /// How this assembly was added to the shared catalog.
    /// </summary>
    public required SharedAssemblySource Source { get; init; }

    /// <summary>
    /// How this assembly matched the shared policy (exact name or prefix rule).
    /// </summary>
    public SharedAssemblyMatchKind MatchKind { get; init; } = SharedAssemblyMatchKind.ExactName;

    /// <summary>
    /// Matched prefix rule when MatchKind is PrefixRule.
    /// </summary>
    public string? MatchedPrefix { get; init; }
    
    /// <summary>
    /// The declared domain type from metadata (if available).
    /// </summary>
    public AssemblyDomainType? DeclaredDomain { get; init; }
    
    /// <summary>
    /// Module ID that provided the manifest hint (only when Source is ManifestHint).
    /// </summary>
    public string? SourceModuleId { get; init; }
    
    /// <summary>
    /// Whether there is a mismatch between declared domain and shared status.
    /// </summary>
    public bool HasMismatch { get; init; }
    
    /// <summary>
    /// Reason for mismatch if HasMismatch is true.
    /// </summary>
    public string? MismatchReason { get; init; }
}

