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
    private readonly ConcurrentDictionary<string, RuntimeModuleHandle> _moduleHandles = new();

    /// <summary>
    /// Gets the identifier of the currently running host (e.g. HostType.Blazor).
    /// </summary>
    public string? HostType { get; private set; }

    /// <summary>
    /// Gets the version of the currently running host.
    /// </summary>
    public Version? HostVersion { get; private set; }

    public IReadOnlyCollection<ModuleDescriptor> Modules => _modules.Values.Select(m => m.Descriptor).ToList();
    
    // Allow access to full runtime info internally or for advanced scenarios
    public IReadOnlyCollection<RuntimeModule> RuntimeModules => _modules.Values.ToList();
    public IReadOnlyCollection<RuntimeModuleHandle> ModuleHandles => _moduleHandles.Values.ToList();

    public IReadOnlyCollection<HostDescriptor> Hosts => _hosts.Values.ToList();

    public void SetCurrentHost(string hostType)
    {
        if (HostType != null && HostType != hostType)
        {
            throw new InvalidOperationException($"Current host is already set to {HostType}.");
        }
        HostType = hostType;
    }

    /// <summary>
    /// Sets the current host version.
    /// </summary>
    /// <param name="version">The host version.</param>
    public void SetHostVersion(Version version)
    {
        ArgumentNullException.ThrowIfNull(version);
        if (HostVersion != null && HostVersion != version)
        {
            throw new InvalidOperationException($"Host version is already set to {HostVersion}.");
        }
        HostVersion = version;
    }

    /// <summary>
    /// Sets the current host version from a version string.
    /// </summary>
    /// <param name="versionString">The version string (e.g., "1.0.0").</param>
    public void SetHostVersion(string versionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(versionString);
        if (!Version.TryParse(versionString, out var version))
        {
            throw new ArgumentException($"Invalid version string: {versionString}", nameof(versionString));
        }
        SetHostVersion(version);
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

    public void RegisterModuleHandle(RuntimeModuleHandle handle)
    {
        ArgumentNullException.ThrowIfNull(handle);
        if (!_moduleHandles.TryAdd(handle.RuntimeModule.Descriptor.Id, handle))
        {
            throw new InvalidOperationException($"Module handle {handle.RuntimeModule.Descriptor.Id} is already registered.");
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

    public RuntimeModuleHandle? RemoveModuleHandle(string moduleId)
    {
        _moduleHandles.TryRemove(moduleId, out var handle);
        return handle;
    }

    public bool TryGetModuleHandle(string moduleId, out RuntimeModuleHandle? handle)
    {
        return _moduleHandles.TryGetValue(moduleId, out handle);
    }
}
