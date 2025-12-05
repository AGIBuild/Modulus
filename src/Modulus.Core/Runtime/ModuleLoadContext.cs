using System.Reflection;
using System.Runtime.Loader;

namespace Modulus.Core.Runtime;

/// <summary>
/// Collectible AssemblyLoadContext per module to allow unloading.
/// </summary>
public sealed class ModuleLoadContext : AssemblyLoadContext
{
    private readonly string _basePath;

    public ModuleLoadContext(string moduleId, string basePath)
        : base(name: moduleId, isCollectible: true)
    {
        _basePath = basePath;
    }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        // 1. Force shared assemblies to be loaded from default context (Host)
        if (IsSharedAssembly(assemblyName))
        {
            return null; // Delegate to default context
        }

        // 2. Try to load from module directory
        var candidatePath = Path.Combine(_basePath, $"{assemblyName.Name}.dll");
        if (File.Exists(candidatePath))
        {
            return LoadFromAssemblyPath(candidatePath);
        }

        return null;
    }

    private static bool IsSharedAssembly(AssemblyName assemblyName)
    {
        var name = assemblyName.Name;
        if (string.IsNullOrEmpty(name)) return false;

        // Core Framework & Extensions
        if (name.StartsWith("System.") ||
            name.StartsWith("Microsoft.") ||
            name.Equals("mscorlib") ||
            name.Equals("netstandard"))
        {
            return true;
        }

        // Modulus Core Assemblies
        if (name.Equals("Modulus.Core") ||
            name.Equals("Modulus.Sdk") ||
            name.Equals("Modulus.UI.Abstractions") ||
            name.Equals("Modulus.UI.Avalonia") ||
            name.Equals("Modulus.UI.Blazor"))
        {
            return true;
        }

        // Avalonia Assemblies (Host should provide these)
        if (name.StartsWith("Avalonia"))
        {
            return true;
        }

        return false;
    }
}
