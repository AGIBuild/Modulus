using System;
using System.Collections.Generic;

namespace Modulus.Core.Architecture;

/// <summary>
/// Canonical shared-assembly policy helpers.
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
    /// Built-in shared assembly prefix presets for well-known hosts.
    /// These are intended for packaging-time exclusions and for host templates.
    /// </summary>
    public static IReadOnlyCollection<string> GetBuiltInPrefixPresetsForHost(string hostId)
    {
        if (string.Equals(hostId, Modulus.Sdk.ModulusHostIds.Avalonia, StringComparison.OrdinalIgnoreCase))
        {
            return new[] { "Avalonia", "AvaloniaUI.", "SkiaSharp", "HarfBuzzSharp" };
        }

        if (string.Equals(hostId, Modulus.Sdk.ModulusHostIds.Blazor, StringComparison.OrdinalIgnoreCase))
        {
            return new[] { "Microsoft.Maui.", "Microsoft.AspNetCore.Components", "MudBlazor" };
        }

        return Array.Empty<string>();
    }

    public static IReadOnlyCollection<string> MergeWithConfiguredPrefixes(IEnumerable<string>? configuredPrefixes, IEnumerable<string>? extraPrefixes = null)
    {
        var merged = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (configuredPrefixes != null)
        {
            foreach (var p in configuredPrefixes)
            {
                if (string.IsNullOrWhiteSpace(p)) continue;
                merged.Add(p.Trim());
            }
        }

        if (extraPrefixes != null)
        {
            foreach (var p in extraPrefixes)
            {
                if (string.IsNullOrWhiteSpace(p)) continue;
                merged.Add(p.Trim());
            }
        }

        return merged.ToList();
    }

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


