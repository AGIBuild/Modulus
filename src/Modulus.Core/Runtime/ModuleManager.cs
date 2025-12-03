using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Modulus.Sdk;

namespace Modulus.Core.Runtime;

public class ModuleManager
{
    private readonly ILogger<ModuleManager> _logger;
    private readonly List<IModule> _modules = new();
    
    public ModuleManager(ILogger<ModuleManager> logger)
    {
        _logger = logger;
    }

    public void AddModule(IModule module)
    {
        _modules.Add(module);
    }

    public IReadOnlyList<IModule> GetModules() => _modules.AsReadOnly();

    public IReadOnlyList<IModule> GetSortedModules()
    {
        // Simple Topological Sort
        var visited = new HashSet<Type>();
        var sorted = new List<IModule>();
        
        // Map Type -> Instance
        var moduleMap = _modules.ToDictionary(m => m.GetType(), m => m);

        void Visit(IModule module)
        {
            var type = module.GetType();
            if (visited.Contains(type)) return;
            visited.Add(type);

            var dependsOnAttrs = type.GetCustomAttributes<DependsOnAttribute>();
            foreach (var attr in dependsOnAttrs)
            {
                foreach (var depType in attr.DependedModuleTypes)
                {
                    if (moduleMap.TryGetValue(depType, out var depModule))
                    {
                        Visit(depModule);
                    }
                }
            }

            sorted.Add(module);
        }

        foreach (var module in _modules)
        {
            Visit(module);
        }

        return sorted;
    }
}

