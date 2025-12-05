namespace Modulus.Architecture;

/// <summary>
/// Declares the domain type of an assembly in the Modulus architecture.
/// This attribute helps developers understand whether an assembly is:
/// - Shared: Loaded once by host, reused by all modules
/// - Module: Loaded in isolated context per module
/// </summary>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false, Inherited = false)]
public sealed class AssemblyDomainAttribute : Attribute
{
    /// <summary>
    /// Gets the domain type of the assembly.
    /// </summary>
    public AssemblyDomainType DomainType { get; }
    
    /// <summary>
    /// Gets an optional description explaining the domain assignment.
    /// </summary>
    public string? Description { get; init; }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="AssemblyDomainAttribute"/> class.
    /// </summary>
    /// <param name="domainType">The domain type of the assembly.</param>
    public AssemblyDomainAttribute(AssemblyDomainType domainType)
    {
        DomainType = domainType;
    }
}

