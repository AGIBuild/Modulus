namespace Modulus.Core.Architecture;

/// <summary>
/// Configuration options for shared assemblies.
/// Bound from "Modulus:Runtime:SharedAssemblies" configuration path.
/// </summary>
public sealed class SharedAssemblyOptions
{
    /// <summary>
    /// Configuration section path.
    /// </summary>
    public const string SectionPath = "Modulus:Runtime:SharedAssemblies";

    /// <summary>
    /// Configuration section path for prefix-based shared assembly rules.
    /// </summary>
    public const string PrefixesSectionPath = "Modulus:Runtime:SharedAssemblyPrefixes";
    
    /// <summary>
    /// Maximum allowed hints from a single module manifest.
    /// </summary>
    public const int MaxManifestHints = 50;
    
    /// <summary>
    /// Maximum length of a single assembly name.
    /// </summary>
    public const int MaxAssemblyNameLength = 256;
    
    /// <summary>
    /// List of assembly names to add to the shared catalog.
    /// These names should be simple assembly names without extension or version.
    /// </summary>
    public List<string> Assemblies { get; set; } = new();

    /// <summary>
    /// List of prefixes to treat as shared assemblies (simple string prefix match).
    /// </summary>
    public List<string> Prefixes { get; set; } = new();
}

