using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Modulus.Sdk;

namespace Modulus.Core.Runtime;

public class ModuleManager
{
    private readonly ILogger<ModuleManager> _logger;
    private readonly List<ModuleRegistration> _registrations = new();
    
    public ModuleManager(ILogger<ModuleManager> logger)
    {
        _logger = logger;
    }

    public void AddModule(IModule module, string? moduleId = null, IReadOnlyCollection<string>? manifestDependencies = null)
    {
        var id = moduleId ?? ResolveModuleId(module.GetType());
        var dependencies = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (manifestDependencies != null)
        {
            foreach (var dep in manifestDependencies)
            {
                dependencies.Add(dep);
            }
        }

        var dependsOnAttrs = module.GetType().GetCustomAttributes<DependsOnAttribute>();
        foreach (var attr in dependsOnAttrs)
        {
            foreach (var depType in attr.DependedModuleTypes)
            {
                dependencies.Add(ResolveModuleId(depType));
            }
        }

        _registrations.Add(new ModuleRegistration(module, id, dependencies));
    }

    public IReadOnlyList<IModule> GetModules() => _registrations.Select(r => r.Module).ToList().AsReadOnly();

    public IReadOnlyList<IModule> GetSortedModules()
    {
        var sorted = ModuleDependencyResolver.TopologicallySort(
            _registrations,
            r => r.Id,
            r => r.Dependencies,
            _logger);

        return sorted.Select(r => r.Module).ToList();
    }

    /// <summary>
    /// Gets sorted modules with their registered IDs.
    /// </summary>
    public IReadOnlyList<(IModule Module, string ModuleId)> GetSortedModulesWithIds()
    {
        var sorted = ModuleDependencyResolver.TopologicallySort(
            _registrations,
            r => r.Id,
            r => r.Dependencies,
            _logger);

        return sorted.Select(r => (r.Module, r.Id)).ToList();
    }

    private static string ResolveModuleId(Type type)
    {
        var attr = type.GetCustomAttribute<ModuleAttribute>();
        if (attr != null && !string.IsNullOrWhiteSpace(attr.Id))
        {
            return attr.Id;
        }

        return type.FullName ?? type.Name;
    }

    private sealed record ModuleRegistration(IModule Module, string Id, HashSet<string> Dependencies);
}

