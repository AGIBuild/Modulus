using System.Collections.Generic;

namespace Modulus.Core.Architecture;

/// <summary>
/// Canonical shared-assembly policy helpers.
/// This is intentionally simple: exact assembly simple names only.
/// </summary>
public static class SharedAssemblyPolicy
{
    private static readonly string[] BuiltInSharedAssemblies =
    [
        // Core Modulus shared assemblies (current)
        "Modulus.Core",
        "Modulus.Sdk",
        "Modulus.UI.Abstractions",
        "Modulus.UI.Avalonia",
        "Modulus.UI.Blazor",

        // Core Modulus shared assemblies (future prefix-naming change)
        "Agibuild.Modulus.Core",
        "Agibuild.Modulus.Sdk",
        "Agibuild.Modulus.UI.Abstractions",
        "Agibuild.Modulus.UI.Avalonia",
        "Agibuild.Modulus.UI.Blazor",

        // Host SDK assemblies (this change)
        "Modulus.HostSdk.Abstractions",
        "Modulus.HostSdk.Runtime"
    ];

    public static IReadOnlyCollection<string> GetBuiltInSharedAssemblies() => BuiltInSharedAssemblies;

    /// <summary>
    /// Merges built-in shared assemblies with host-provided configuration.
    /// </summary>
    public static IReadOnlyCollection<string> MergeWithConfiguredAssemblies(IEnumerable<string>? configuredAssemblies)
    {
        var merged = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var name in BuiltInSharedAssemblies)
        {
            merged.Add(name);
        }

        if (configuredAssemblies != null)
        {
            foreach (var name in configuredAssemblies)
            {
                if (string.IsNullOrWhiteSpace(name)) continue;
                merged.Add(name.Trim());
            }
        }

        return merged.ToList();
    }
}


