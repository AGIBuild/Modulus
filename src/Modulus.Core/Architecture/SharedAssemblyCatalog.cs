using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Modulus.Architecture;

namespace Modulus.Core.Architecture;

public interface ISharedAssemblyCatalog
{
    IReadOnlyCollection<string> Names { get; }
    bool IsShared(AssemblyName assemblyName);
}

/// <summary>
/// Catalog of assemblies that must be resolved from the shared domain.
/// Built from assembly-domain metadata in the default load context.
/// </summary>
public sealed class SharedAssemblyCatalog : ISharedAssemblyCatalog
{
    private readonly HashSet<string> _sharedNames;
    private readonly Dictionary<string, AssemblyDomainType> _domainMap;
    private readonly ILogger<SharedAssemblyCatalog>? _logger;

    private SharedAssemblyCatalog(HashSet<string> sharedNames, Dictionary<string, AssemblyDomainType> domainMap, ILogger<SharedAssemblyCatalog>? logger)
    {
        _sharedNames = sharedNames;
        _domainMap = domainMap;
        _logger = logger;
    }

    public static SharedAssemblyCatalog FromAssemblies(IEnumerable<Assembly> assemblies, IEnumerable<string>? additionalNames = null, ILogger<SharedAssemblyCatalog>? logger = null)
    {
        var sharedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var domainMap = new Dictionary<string, AssemblyDomainType>(StringComparer.OrdinalIgnoreCase);

        foreach (var assembly in assemblies)
        {
            var name = assembly.GetName().Name;
            if (string.IsNullOrEmpty(name))
            {
                continue;
            }

            var domain = AssemblyDomainInfo.GetDomainType(assembly);
            domainMap[name] = domain;

            if (domain == AssemblyDomainType.Shared)
            {
                sharedNames.Add(name);
            }
        }

        if (additionalNames != null)
        {
            foreach (var extra in additionalNames)
            {
                if (!string.IsNullOrWhiteSpace(extra))
                {
                    sharedNames.Add(extra);
                }
            }
        }

        return new SharedAssemblyCatalog(sharedNames, domainMap, logger);
    }

    public IReadOnlyCollection<string> Names => _sharedNames;

    public bool IsShared(AssemblyName assemblyName)
    {
        if (assemblyName.Name is null)
        {
            return false;
        }

        var isShared = _sharedNames.Contains(assemblyName.Name);
        if (isShared && _domainMap.TryGetValue(assemblyName.Name, out var domain) && domain != AssemblyDomainType.Shared)
        {
            _logger?.LogWarning("Assembly {Assembly} is marked shared but declared as {Domain}.", assemblyName.Name, domain);
        }

        return isShared;
    }
}

