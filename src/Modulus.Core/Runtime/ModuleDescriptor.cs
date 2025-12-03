using System.Collections.Generic;

namespace Modulus.Core.Runtime;

/// <summary>
/// Describes a module that has been discovered/loaded.
/// </summary>
public sealed class ModuleDescriptor
{
    public string Id { get; }

    public string DisplayName { get; }

    public string Description { get; }

    public string Version { get; }

    public IReadOnlyCollection<string> SupportedHosts { get; }

    public ModuleDescriptor(
        string id, 
        string version, 
        string? displayName = null, 
        string? description = null,
        IReadOnlyCollection<string>? supportedHosts = null)
    {
        Id = id;
        Version = version;
        DisplayName = displayName ?? id;
        Description = description ?? string.Empty;
        SupportedHosts = supportedHosts ?? Array.Empty<string>();
    }
}

