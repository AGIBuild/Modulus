namespace Modulus.Core.Runtime;

/// <summary>
/// Describes a host (Blazor, Avalonia, etc.) known to the runtime.
/// </summary>
public sealed class HostDescriptor
{
    public string Id { get; }

    public string HostType { get; }

    public HostDescriptor(string id, string hostType)
    {
        Id = id;
        HostType = hostType;
    }
}

