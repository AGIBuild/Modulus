using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Modulus.Core.Runtime;

/// <summary>
/// In-memory representation of the active modules and hosts.
/// </summary>
public sealed class RuntimeContext
{
    // Use ConcurrentDictionary for thread safety
    private readonly ConcurrentDictionary<string, RuntimeModule> _modules = new();
    private readonly ConcurrentDictionary<string, HostDescriptor> _hosts = new();

    /// <summary>
    /// Gets the identifier of the currently running host (e.g. HostType.Blazor).
    /// </summary>
    public string? HostType { get; private set; }

    public IReadOnlyCollection<ModuleDescriptor> Modules => _modules.Values.Select(m => m.Descriptor).ToList();
    
    // Allow access to full runtime info internally or for advanced scenarios
    public IReadOnlyCollection<RuntimeModule> RuntimeModules => _modules.Values.ToList();

    public IReadOnlyCollection<HostDescriptor> Hosts => _hosts.Values.ToList();

    public void SetCurrentHost(string hostType)
    {
        if (HostType != null && HostType != hostType)
        {
            throw new InvalidOperationException($"Current host is already set to {HostType}.");
        }
        HostType = hostType;
    }

    public void RegisterHost(HostDescriptor host)
    {
        _hosts.TryAdd(host.Id, host);
    }
    
    public void UnregisterHost(string hostId)
    {
        _hosts.TryRemove(hostId, out _);
    }

    public void RegisterModule(RuntimeModule module)
    {
        if (!_modules.TryAdd(module.Descriptor.Id, module))
        {
            throw new InvalidOperationException($"Module {module.Descriptor.Id} is already registered.");
        }
    }

    public bool TryGetModule(string moduleId, out RuntimeModule? module)
    {
        return _modules.TryGetValue(moduleId, out module);
    }

    public RuntimeModule? RemoveModule(string moduleId)
    {
        _modules.TryRemove(moduleId, out var module);
        return module;
    }
}
