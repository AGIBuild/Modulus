using Modulus.Architecture;

namespace Modulus.Core.Architecture;

/// <summary>
/// Records a mismatched shared assembly request.
/// </summary>
public sealed record SharedAssemblyMismatch
{
    /// <summary>
    /// Simple assembly name.
    /// </summary>
    public required string AssemblyName { get; init; }
    
    /// <summary>
    /// Module that requested this assembly.
    /// </summary>
    public string? ModuleId { get; init; }
    
    /// <summary>
    /// How the request was made (config, manifest hint, etc.).
    /// </summary>
    public SharedAssemblySource RequestSource { get; init; }
    
    /// <summary>
    /// The actual declared domain if known.
    /// </summary>
    public AssemblyDomainType? DeclaredDomain { get; init; }
    
    /// <summary>
    /// Reason for the mismatch.
    /// </summary>
    public required string Reason { get; init; }
    
    /// <summary>
    /// Timestamp when this mismatch was recorded.
    /// </summary>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}

