using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Modulus.Sdk;

namespace Modulus.Core.Runtime;

/// <summary>
/// Resolves dependency ordering for ModulusComponent types using [DependsOn] attributes.
/// </summary>
internal static class ComponentDependencyResolver
{
    public static IReadOnlyList<Type> TopologicallySort(
        IEnumerable<Type> components,
        ILogger? logger = null)
    {
        var idComparer = StringComparer.OrdinalIgnoreCase;
        var nodes = components.ToDictionary(t => t.FullName ?? t.Name, t => t, idComparer);
        var indegrees = new Dictionary<string, int>(idComparer);
        var edges = new Dictionary<string, List<string>>(idComparer);

        foreach (var (id, type) in nodes)
        {
            indegrees[id] = 0;
            var deps = GetDependencies(type);
            foreach (var depType in deps)
            {
                var depId = depType.FullName ?? depType.Name;
                if (depId == null) continue;

                if (!nodes.ContainsKey(depId))
                {
                    logger?.LogError("Dependency '{DependencyId}' for component '{ComponentId}' not found.", depId, id);
                    throw new InvalidOperationException($"Missing dependency '{depId}' for component '{id}'.");
                }
                if (!edges.ContainsKey(depId))
                {
                    edges[depId] = new List<string>();
                }
                edges[depId].Add(id);
            }
        }

        foreach (var (id, type) in nodes)
        {
            var deps = GetDependencies(type);
            foreach (var depType in deps)
            {
                var depId = depType.FullName ?? depType.Name;
                if (depId != null && indegrees.ContainsKey(id))
                {
                    indegrees[id]++;
                }
            }
        }

        var queue = new Queue<string>(indegrees.Where(pair => pair.Value == 0).Select(pair => pair.Key));
        var sortedList = new List<Type>();

        while (queue.Count > 0)
        {
            var currentId = queue.Dequeue();
            sortedList.Add(nodes[currentId]);

            if (edges.TryGetValue(currentId, out var dependentIds))
            {
                foreach (var dependentId in dependentIds)
                {
                    indegrees[dependentId]--;
                    if (indegrees[dependentId] == 0)
                    {
                        queue.Enqueue(dependentId);
                    }
                }
            }
        }

        if (sortedList.Count != nodes.Count)
        {
            var remainingNodes = nodes.Keys.Except(sortedList.Select(t => t.FullName ?? t.Name)).ToList();
            logger?.LogError("Circular dependency detected or missing components: {RemainingNodes}", string.Join(", ", remainingNodes));
            throw new InvalidOperationException($"Circular dependency detected or missing components: {string.Join(", ", remainingNodes)}");
        }

        return sortedList;
    }

    private static IReadOnlyCollection<Type> GetDependencies(Type componentType)
    {
        var deps = new List<Type>();
        var dependsOnAttrs = componentType.GetCustomAttributes<DependsOnAttribute>();
        foreach (var attr in dependsOnAttrs)
        {
            deps.AddRange(attr.DependedModuleTypes);
        }
        return deps;
    }
}

