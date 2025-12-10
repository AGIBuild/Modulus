using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Modulus.Core.Runtime;

/// <summary>
/// Manifest containing all bundled modules, generated at build time from extension.vsixmanifest files.
/// </summary>
public class BundledModulesManifest
{
    /// <summary>
    /// Timestamp when the manifest was generated.
    /// </summary>
    [JsonPropertyName("generatedAt")]
    public DateTime GeneratedAt { get; set; }

    /// <summary>
    /// Build configuration (Debug/Release).
    /// </summary>
    [JsonPropertyName("configuration")]
    public string? Configuration { get; set; }

    /// <summary>
    /// List of bundled modules.
    /// </summary>
    [JsonPropertyName("modules")]
    public List<BundledModuleJson> Modules { get; set; } = [];

    /// <summary>
    /// Loads the manifest from an embedded resource.
    /// </summary>
    /// <param name="assembly">Assembly containing the embedded resource.</param>
    /// <param name="resourceName">Resource name (e.g., "bundled-modules.json").</param>
    /// <returns>The loaded manifest, or null if not found.</returns>
    public static BundledModulesManifest? LoadFromEmbeddedResource(Assembly assembly, string resourceName = "bundled-modules.json")
    {
        // Find the resource with partial name match
        var fullResourceName = Array.Find(
            assembly.GetManifestResourceNames(),
            name => name.EndsWith(resourceName, StringComparison.OrdinalIgnoreCase));

        if (fullResourceName == null)
            return null;

        using var stream = assembly.GetManifestResourceStream(fullResourceName);
        if (stream == null)
            return null;

        using var reader = new StreamReader(stream);
        var json = reader.ReadToEnd();
        
        return JsonSerializer.Deserialize<BundledModulesManifest>(json, JsonOptions);
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };
}

/// <summary>
/// JSON representation of a bundled module.
/// </summary>
public class BundledModuleJson
{
    // Identity fields
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("version")]
    public string Version { get; set; } = "1.0.0";

    [JsonPropertyName("language")]
    public string? Language { get; set; }

    [JsonPropertyName("publisher")]
    public string? Publisher { get; set; }

    // Metadata fields
    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("tags")]
    public string? Tags { get; set; }

    [JsonPropertyName("website")]
    public string? Website { get; set; }

    // Installation fields
    [JsonPropertyName("supportedHosts")]
    public List<string> SupportedHosts { get; set; } = [];

    [JsonPropertyName("dependencies")]
    public List<ModuleDependencyJson>? Dependencies { get; set; }

    // Build fields
    [JsonPropertyName("path")]
    public string Path { get; set; } = string.Empty;

    [JsonPropertyName("isBundled")]
    public bool IsBundled { get; set; } = true;

    // Menus by host
    [JsonPropertyName("menus")]
    public Dictionary<string, List<MenuJson>> Menus { get; set; } = [];

    /// <summary>
    /// Converts to BundledModuleDefinition.
    /// </summary>
    public BundledModuleDefinition ToDefinition()
    {
        var menusByHost = new Dictionary<string, IReadOnlyList<MenuDefinition>>();
        foreach (var (hostId, menus) in Menus)
        {
            menusByHost[hostId] = menus.ConvertAll(m => m.ToDefinition());
        }

        return new BundledModuleDefinition
        {
            Id = Id,
            Version = Version,
            Language = Language,
            Publisher = Publisher,
            DisplayName = DisplayName,
            Description = Description,
            Tags = Tags,
            Website = Website,
            SupportedHosts = SupportedHosts,
            Dependencies = Dependencies?.ConvertAll(d => d.ToDefinition()) ?? [],
            Path = Path,
            IsBundled = IsBundled,
            MenusByHost = menusByHost
        };
    }
}

/// <summary>
/// JSON representation of a module dependency.
/// </summary>
public class ModuleDependencyJson
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("versionRange")]
    public string? VersionRange { get; set; }

    public ModuleDependency ToDefinition() => new()
    {
        Id = Id,
        VersionRange = VersionRange
    };
}

/// <summary>
/// JSON representation of a menu item.
/// </summary>
public class MenuJson
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; } = string.Empty;

    [JsonPropertyName("icon")]
    public string Icon { get; set; } = "Folder";

    [JsonPropertyName("route")]
    public string Route { get; set; } = string.Empty;

    [JsonPropertyName("location")]
    public string Location { get; set; } = "Main";

    [JsonPropertyName("order")]
    public int Order { get; set; }

    public MenuDefinition ToDefinition() => new()
    {
        Id = Id,
        DisplayName = DisplayName,
        Icon = Icon,
        Route = Route,
        Location = Enum.TryParse<Modulus.UI.Abstractions.MenuLocation>(Location, true, out var loc) 
            ? loc 
            : Modulus.UI.Abstractions.MenuLocation.Main,
        Order = Order
    };
}

