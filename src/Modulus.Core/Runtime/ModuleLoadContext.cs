using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using Microsoft.Extensions.Logging;
using Modulus.Core.Architecture;

namespace Modulus.Core.Runtime;

/// <summary>
/// Collectible AssemblyLoadContext per module to allow unloading.
/// </summary>
public sealed class ModuleLoadContext : AssemblyLoadContext
{
    private readonly string _basePath;
    private readonly ISharedAssemblyCatalog _sharedAssemblies;
    private readonly ILogger? _logger;

    public ModuleLoadContext(string moduleId, string basePath, ISharedAssemblyCatalog sharedAssemblies, ILogger? logger = null)
        : base(name: moduleId, isCollectible: true)
    {
        _basePath = basePath;
        _sharedAssemblies = sharedAssemblies;
        _logger = logger;
    }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        // 1. Force shared assemblies to be loaded from default context (Host)
        if (_sharedAssemblies.IsShared(assemblyName))
        {
            var defaultAssembly = AssemblyLoadContext.Default.Assemblies.FirstOrDefault(a =>
                string.Equals(a.GetName().Name, assemblyName.Name, StringComparison.OrdinalIgnoreCase));
            if (defaultAssembly != null && AssemblyDomainInfo.GetDomainType(defaultAssembly) == Modulus.Architecture.AssemblyDomainType.Module)
            {
                _logger?.LogWarning("Assembly {Assembly} is requested as shared but declared Module.", assemblyName.Name);
            }
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

}
