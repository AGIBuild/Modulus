namespace Modulus.Architecture;

/// <summary>
/// Defines the assembly domain type in the Modulus architecture.
/// </summary>
public enum AssemblyDomainType
{
    /// <summary>
    /// Unknown or unspecified domain type.
    /// </summary>
    Unknown = 0,
    
    /// <summary>
    /// Shared domain - loaded by host, reused by all modules.
    /// Types from shared assemblies are consistent across all contexts.
    /// Examples: Modulus.Core, Modulus.Sdk, Modulus.UI.Abstractions
    /// </summary>
    Shared = 1,
    
    /// <summary>
    /// Module domain - loaded in isolated AssemblyLoadContext per module.
    /// Each module has its own instance of these assemblies.
    /// Examples: EchoPlugin.*, ComponentsDemo.*
    /// </summary>
    Module = 2
}

