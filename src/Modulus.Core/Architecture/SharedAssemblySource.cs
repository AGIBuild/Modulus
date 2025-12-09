namespace Modulus.Core.Architecture;

/// <summary>
/// Identifies the source that added an assembly to the shared catalog.
/// </summary>
public enum SharedAssemblySource
{
    /// <summary>
    /// Added via assembly [AssemblyDomain(Shared)] attribute.
    /// </summary>
    DomainAttribute,
    
    /// <summary>
    /// Added via host configuration (appsettings/environment).
    /// </summary>
    HostConfig,
    
    /// <summary>
    /// Added via module manifest sharedAssemblyHints.
    /// </summary>
    ManifestHint
}

