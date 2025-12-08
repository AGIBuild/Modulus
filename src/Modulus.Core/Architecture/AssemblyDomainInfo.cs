using System.Reflection;
using System.Runtime.Loader;
using Modulus.Architecture;

namespace Modulus.Core.Architecture;

/// <summary>
/// Provides runtime information about assembly domains in the Modulus architecture.
/// </summary>
public static class AssemblyDomainInfo
{
    /// <summary>
    /// Gets the domain type of the specified assembly.
    /// First checks for [AssemblyDomain] attribute, then falls back to known assemblies list.
    /// </summary>
    public static AssemblyDomainType GetDomainType(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        
        // Check for explicit attribute
        var attr = assembly.GetCustomAttribute<AssemblyDomainAttribute>();
        if (attr != null)
        {
            return attr.DomainType;
        }
        
        // Check if loaded in default context (likely shared) or isolated context (module)
        var context = AssemblyLoadContext.GetLoadContext(assembly);
        if (context == AssemblyLoadContext.Default)
        {
            return AssemblyDomainType.Unknown;
        }
        
        return AssemblyDomainType.Module;
    }

    /// <summary>
    /// Gets the domain type of the calling assembly.
    /// </summary>
    public static AssemblyDomainType GetCallingAssemblyDomainType()
    {
        return GetDomainType(Assembly.GetCallingAssembly());
    }

    /// <summary>
    /// Checks if the specified assembly is in the shared domain.
    /// </summary>
    public static bool IsSharedAssembly(Assembly assembly)
    {
        return GetDomainType(assembly) == AssemblyDomainType.Shared;
    }

    /// <summary>
    /// Checks if the specified assembly is in a module domain.
    /// </summary>
    public static bool IsModuleAssembly(Assembly assembly)
    {
        return GetDomainType(assembly) == AssemblyDomainType.Module;
    }

    /// <summary>
    /// Checks if the calling code is running in the shared domain.
    /// </summary>
    public static bool IsInSharedDomain()
    {
        var callingAssembly = Assembly.GetCallingAssembly();
        var context = AssemblyLoadContext.GetLoadContext(callingAssembly);
        return context == AssemblyLoadContext.Default || IsSharedAssembly(callingAssembly);
    }

    /// <summary>
    /// Checks if the calling code is running in a module domain.
    /// </summary>
    public static bool IsInModuleDomain()
    {
        var callingAssembly = Assembly.GetCallingAssembly();
        var context = AssemblyLoadContext.GetLoadContext(callingAssembly);
        return context != AssemblyLoadContext.Default && context?.Name?.StartsWith("Module_") == true;
    }

    /// <summary>
    /// Gets the module ID if running in a module domain, otherwise returns null.
    /// </summary>
    public static string? GetCurrentModuleId()
    {
        var callingAssembly = Assembly.GetCallingAssembly();
        var context = AssemblyLoadContext.GetLoadContext(callingAssembly);
        
        if (context == AssemblyLoadContext.Default)
            return null;
            
        var contextName = context?.Name;
        if (contextName?.StartsWith("Module_") == true)
        {
            return contextName.Substring("Module_".Length);
        }
        
        return null;
    }

    /// <summary>
    /// Gets the AssemblyLoadContext name for the specified assembly.
    /// </summary>
    public static string? GetLoadContextName(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        return AssemblyLoadContext.GetLoadContext(assembly)?.Name;
    }

    /// <summary>
    /// Gets diagnostic information about the assembly domain.
    /// </summary>
    public static AssemblyDomainDiagnostics GetDiagnostics(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        
        var context = AssemblyLoadContext.GetLoadContext(assembly);
        var attr = assembly.GetCustomAttribute<AssemblyDomainAttribute>();
        
        return new AssemblyDomainDiagnostics
        {
            AssemblyName = assembly.GetName().Name ?? "Unknown",
            AssemblyFullName = assembly.FullName ?? "Unknown",
            DeclaredDomainType = attr?.DomainType ?? AssemblyDomainType.Unknown,
            EffectiveDomainType = GetDomainType(assembly),
            LoadContextName = context?.Name ?? "Unknown",
            IsDefaultContext = context == AssemblyLoadContext.Default,
            HasDomainAttribute = attr != null,
            Description = attr?.Description
        };
    }

    /// <summary>
    /// Validates that the assembly domain declaration matches the runtime context.
    /// Returns true if valid, false if there's a mismatch.
    /// </summary>
    public static bool ValidateDomainDeclaration(Assembly assembly, out string? errorMessage)
    {
        var diagnostics = GetDiagnostics(assembly);
        
        if (!diagnostics.HasDomainAttribute)
        {
            errorMessage = $"Assembly '{diagnostics.AssemblyName}' is missing [AssemblyDomain] attribute.";
            return false;
        }
        
        // Shared assembly should be in default context
        if (diagnostics.DeclaredDomainType == AssemblyDomainType.Shared && !diagnostics.IsDefaultContext)
        {
            errorMessage = $"Assembly '{diagnostics.AssemblyName}' is declared as Shared but loaded in isolated context '{diagnostics.LoadContextName}'.";
            return false;
        }
        
        // Module assembly should NOT be in default context (unless during development)
        if (diagnostics.DeclaredDomainType == AssemblyDomainType.Module && diagnostics.IsDefaultContext)
        {
            // This is a warning, not an error - modules may be loaded in default context during testing
            errorMessage = $"Assembly '{diagnostics.AssemblyName}' is declared as Module but loaded in default context.";
            return true; // Still valid, just a warning
        }
        
        errorMessage = null;
        return true;
    }
}

/// <summary>
/// Diagnostic information about an assembly's domain configuration.
/// </summary>
public sealed class AssemblyDomainDiagnostics
{
    /// <summary>
    /// The simple name of the assembly.
    /// </summary>
    public required string AssemblyName { get; init; }
    
    /// <summary>
    /// The full name of the assembly.
    /// </summary>
    public required string AssemblyFullName { get; init; }
    
    /// <summary>
    /// The domain type declared via [AssemblyDomain] attribute.
    /// </summary>
    public AssemblyDomainType DeclaredDomainType { get; init; }
    
    /// <summary>
    /// The effective domain type determined at runtime.
    /// </summary>
    public AssemblyDomainType EffectiveDomainType { get; init; }
    
    /// <summary>
    /// The name of the AssemblyLoadContext.
    /// </summary>
    public required string LoadContextName { get; init; }
    
    /// <summary>
    /// Whether the assembly is loaded in the default context.
    /// </summary>
    public bool IsDefaultContext { get; init; }
    
    /// <summary>
    /// Whether the assembly has the [AssemblyDomain] attribute.
    /// </summary>
    public bool HasDomainAttribute { get; init; }
    
    /// <summary>
    /// Optional description from the attribute.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Returns a formatted diagnostic string.
    /// </summary>
    public override string ToString()
    {
        return $"""
            Assembly: {AssemblyName}
            Declared Domain: {DeclaredDomainType}
            Effective Domain: {EffectiveDomainType}
            Load Context: {LoadContextName}
            Is Default Context: {IsDefaultContext}
            Has Attribute: {HasDomainAttribute}
            """;
    }
}

