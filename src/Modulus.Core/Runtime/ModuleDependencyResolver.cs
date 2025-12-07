using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace Modulus.Core.Runtime;

internal static class ModuleDependencyResolver
{
    public static IReadOnlyList<T> TopologicallySort<T>(
        IEnumerable<T> items,
        Func<T, string> getId,
        Func<T, IReadOnlyCollection<string>> getDependencies,
        ILogger? logger = null)
    {
        var idComparer = StringComparer.OrdinalIgnoreCase;
        var nodes = items.ToDictionary(getId, item => item, idComparer);
        var indegrees = new Dictionary<string, int>(idComparer);
        var edges = new Dictionary<string, List<string>>(idComparer);

        foreach (var (id, item) in nodes)
        {
            var deps = getDependencies(item);
            indegrees[id] = indegrees.GetValueOrDefault(id);

            foreach (var dep in deps)
            {
                if (string.IsNullOrWhiteSpace(dep))
                {
                    continue;
                }

                if (!nodes.ContainsKey(dep))
                {
                    logger?.LogError("Missing dependency {Dependency} for module {Module}.", dep, id);
                    throw new InvalidOperationException($"Missing dependency '{dep}' for module '{id}'.");
                }

                indegrees[id] = indegrees.GetValueOrDefault(id) + 1;
                var dependents = edges.GetValueOrDefault(dep);
                if (dependents == null)
                {
                    dependents = new List<string>();
                    edges[dep] = dependents;
                }
                dependents.Add(id);
            }
        }

        var queue = new Queue<string>(indegrees.Where(kvp => kvp.Value == 0).Select(kvp => kvp.Key));
        var ordered = new List<T>();

        while (queue.TryDequeue(out var current))
        {
            ordered.Add(nodes[current]);

            if (!edges.TryGetValue(current, out var dependents))
            {
                continue;
            }

            foreach (var dependent in dependents)
            {
                indegrees[dependent]--;
                if (indegrees[dependent] == 0)
                {
                    queue.Enqueue(dependent);
                }
            }
        }

        if (ordered.Count != nodes.Count)
        {
            var remaining = indegrees.Where(kvp => kvp.Value > 0).Select(kvp => kvp.Key).ToList();
            logger?.LogError("Detected cyclic dependency among modules: {Modules}", string.Join(", ", remaining));
            throw new InvalidOperationException("Detected cyclic dependency among modules: " + string.Join(", ", remaining));
        }

        return ordered;
    }
}

