using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Modulus.Sdk;

/// <summary>
/// Strongly typed representation of manifest.json inside a .modpkg package.
/// </summary>
public sealed class ModuleManifest
{
    [JsonPropertyName("manifestVersion")]
    public string ManifestVersion { get; init; } = "1.0";

    /// <summary>
    /// Unique module identifier. Should be a GUID to avoid naming conflicts.
    /// Example: "a1b2c3d4-e5f6-4a7b-8c9d-0e1f2a3b4c5d"
    /// </summary>
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("version")]
    public required string Version { get; init; }

    [JsonPropertyName("displayName")]
    public string? DisplayName { get; init; }

    [JsonPropertyName("description")]
    public string? Description { get; init; }

    [JsonPropertyName("supportedHosts")]
    public List<string> SupportedHosts { get; init; } = new();

    [JsonPropertyName("coreAssemblies")]
    public List<string> CoreAssemblies { get; init; } = new();

    [JsonPropertyName("uiAssemblies")]
    public Dictionary<string, List<string>> UiAssemblies { get; init; } = new();

    [JsonPropertyName("dependencies")]
    public Dictionary<string, string> Dependencies { get; init; } = new();

    [JsonPropertyName("assemblyHashes")]
    public Dictionary<string, string> AssemblyHashes { get; init; } = new();

    [JsonPropertyName("signature")]
    public ManifestSignature? Signature { get; init; }
}

