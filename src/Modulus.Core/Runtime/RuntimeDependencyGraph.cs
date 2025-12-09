using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Modulus.Sdk;
using NuGet.Versioning;

namespace Modulus.Core.Runtime;

/// <summary>
/// Builds a unified dependency graph across loaded runtime modules using
/// manifest dependencies (id + version range) and [DependsOn] attributes
/// declared on IModule implementations.
/// </summary>
public static class RuntimeDependencyGraph
{
    public static IReadOnlyList<RuntimeModuleHandle> TopologicallySort(
        IEnumerable<RuntimeModuleHandle> handles,
        ILogger? logger = null)
    {
        var handleList = handles.ToList();
        if (handleList.Count == 0)
        {
            return Array.Empty<RuntimeModuleHandle>();
        }

        // Map module instance type -> module id for fast resolution of DependsOn targets.
        var typeOwnerMap = BuildTypeOwnerMap(handleList);

        var nodes = new List<Node>(handleList.Count);
        foreach (var handle in handleList)
        {
            var moduleId = handle.RuntimeModule.Descriptor.Id;
            var deps = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // 1) Manifest dependency edges (id + version range).
            foreach (var dep in handle.Manifest.Dependencies)
            {
                ValidateDependencyVersion(handle, dep.Id, dep.Version, handleList, logger);
                deps.Add(dep.Id);
            }

            // 2) [DependsOn] edges between modules.
            foreach (var moduleInstance in handle.ModuleInstances)
            {
                var dependsOnAttrs = moduleInstance.GetType().GetCustomAttributes<DependsOnAttribute>();
                foreach (var attr in dependsOnAttrs)
                {
                    foreach (var depType in attr.DependedModuleTypes)
                    {
                        if (!TryResolveModuleForType(depType, typeOwnerMap, out var targetModuleId))
                        {
                            logger?.LogError("Module {ModuleId} depends on {DependencyType} which is not loaded.", moduleId, depType.FullName ?? depType.Name);
                            throw new InvalidOperationException($"Missing dependency type '{depType.FullName ?? depType.Name}' for module '{moduleId}'.");
                        }
                        
                        // Skip self-dependency (UI components depending on Core within the same module package)
                        if (string.Equals(targetModuleId, moduleId, StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }
                        
                        deps.Add(targetModuleId);
                    }
                }
            }

            nodes.Add(new Node(handle, deps));
        }

        var sorted = ModuleDependencyResolver.TopologicallySort(
            nodes,
            n => n.Handle.RuntimeModule.Descriptor.Id,
            n => n.Dependencies,
            logger);

        return sorted.Select(n => n.Handle).ToList();
    }

    private static void ValidateDependencyVersion(
        RuntimeModuleHandle sourceHandle,
        string dependencyId,
        string dependencyRange,
        IReadOnlyList<RuntimeModuleHandle> handles,
        ILogger? logger)
    {
        var sourceId = sourceHandle.RuntimeModule.Descriptor.Id;
        var targetHandle = handles.FirstOrDefault(h => dependencyId.Equals(h.RuntimeModule.Descriptor.Id, StringComparison.OrdinalIgnoreCase));
        if (targetHandle == null)
        {
            logger?.LogError("Module {ModuleId} declares dependency {DependencyId} which is not loaded.", sourceId, dependencyId);
            throw new InvalidOperationException($"Missing dependency '{dependencyId}' for module '{sourceId}'.");
        }

        if (!NuGetVersion.TryParse(targetHandle.RuntimeModule.Descriptor.Version, out var dependencyVersion))
        {
            logger?.LogError("Module {ModuleId} dependency {DependencyId} has invalid version {DependencyVersion}.", sourceId, dependencyId, targetHandle.RuntimeModule.Descriptor.Version);
            throw new InvalidOperationException($"Dependency '{dependencyId}' version '{targetHandle.RuntimeModule.Descriptor.Version}' is not a valid SemVer.");
        }

        if (!VersionRange.TryParse(dependencyRange, out var range))
        {
            logger?.LogError("Module {ModuleId} dependency {DependencyId} has invalid version range {Range}.", sourceId, dependencyId, dependencyRange);
            throw new InvalidOperationException($"Dependency '{dependencyId}' range '{dependencyRange}' is not a valid SemVer range.");
        }

        if (!range.Satisfies(dependencyVersion))
        {
            logger?.LogError("Module {ModuleId} dependency {DependencyId} version {DependencyVersion} does not satisfy range {Range}.", sourceId, dependencyId, dependencyVersion, dependencyRange);
            throw new InvalidOperationException($"Dependency '{dependencyId}' version '{dependencyVersion}' does not satisfy '{dependencyRange}'.");
        }
    }

    private static bool TryResolveModuleForType(
        Type dependencyType,
        IReadOnlyDictionary<Type, string> typeOwnerMap,
        out string moduleId)
    {
        moduleId = string.Empty;

        if (typeOwnerMap.TryGetValue(dependencyType, out var mapped))
        {
            moduleId = mapped;
            return true;
        }

        // Fallback by FullName match in case types come from different load contexts but share name.
        var match = typeOwnerMap.FirstOrDefault(kvp => string.Equals(kvp.Key.FullName, dependencyType.FullName, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrEmpty(match.Value))
        {
            moduleId = match.Value!;
            return true;
        }

        return false;
    }

    private static Dictionary<Type, string> BuildTypeOwnerMap(IEnumerable<RuntimeModuleHandle> handles)
    {
        var map = new Dictionary<Type, string>();
        foreach (var handle in handles)
        {
            var moduleId = handle.RuntimeModule.Descriptor.Id;
            foreach (var moduleInstance in handle.ModuleInstances)
            {
                var type = moduleInstance.GetType();
                // Last write wins; assemblies should be unique per module but we prefer predictable behavior.
                map[type] = moduleId;
            }
        }
        return map;
    }

    private sealed record Node(RuntimeModuleHandle Handle, HashSet<string> Dependencies);
}

