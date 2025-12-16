using System.Security.Cryptography;
using System.Xml.Linq;
using Modulus.Sdk;

namespace Modulus.Core.Manifest;

/// <summary>
/// Reads extension.vsixmanifest (XML format) files.
/// </summary>
public static class VsixManifestReader
{
    private static readonly XNamespace VsixNs = "http://schemas.microsoft.com/developer/vsx-schema/2011";

    /// <summary>
    /// Reads a VsixManifest from a stream.
    /// </summary>
    public static async Task<VsixManifest?> ReadAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        var doc = await XDocument.LoadAsync(stream, LoadOptions.None, cancellationToken);
        return Parse(doc);
    }

    /// <summary>
    /// Reads a VsixManifest from a file.
    /// </summary>
    public static async Task<VsixManifest?> ReadFromFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(filePath))
        {
            return null;
        }

        await using var stream = File.OpenRead(filePath);
        return await ReadAsync(stream, cancellationToken);
    }

    /// <summary>
    /// Computes SHA256 hash of a manifest file for change detection.
    /// </summary>
    public static async Task<string> ComputeHashAsync(string filePath, CancellationToken cancellationToken = default)
    {
        await using var stream = File.OpenRead(filePath);
        var hashBytes = await SHA256.HashDataAsync(stream, cancellationToken);
        return Convert.ToHexString(hashBytes);
    }

    private static VsixManifest? Parse(XDocument doc)
    {
        var root = doc.Root;
        if (root == null) return null;

        // Support both namespaced and non-namespaced elements
        var ns = root.GetDefaultNamespace();
        if (ns == XNamespace.None)
        {
            ns = VsixNs;
        }

        var version = (string?)root.Attribute("Version") ?? "2.0.0";

        // Parse Metadata
        var metadataEl = root.Element(ns + "Metadata") ?? root.Element("Metadata");
        if (metadataEl == null) return null;

        var identityEl = metadataEl.Element(ns + "Identity") ?? metadataEl.Element("Identity");
        if (identityEl == null) return null;

        var identity = new ManifestIdentity
        {
            Id = (string?)identityEl.Attribute("Id") ?? "",
            Version = (string?)identityEl.Attribute("Version") ?? "1.0.0",
            Publisher = (string?)identityEl.Attribute("Publisher") ?? "",
            Language = (string?)identityEl.Attribute("Language") ?? "en-US"
        };

        var metadata = new ManifestMetadata
        {
            Identity = identity,
            DisplayName = GetElementValue(metadataEl, ns, "DisplayName") ?? identity.Id,
            Description = GetElementValue(metadataEl, ns, "Description"),
            Icon = GetElementValue(metadataEl, ns, "Icon"),
            Tags = GetElementValue(metadataEl, ns, "Tags"),
            MoreInfo = GetElementValue(metadataEl, ns, "MoreInfo"),
            License = GetElementValue(metadataEl, ns, "License")
        };

        // Parse Installation targets
        var installationEl = root.Element(ns + "Installation") ?? root.Element("Installation");
        var installationTargets = new List<InstallationTarget>();
        if (installationEl != null)
        {
            foreach (var targetEl in installationEl.Elements(ns + "InstallationTarget").Concat(installationEl.Elements("InstallationTarget")))
            {
                installationTargets.Add(new InstallationTarget
                {
                    Id = (string?)targetEl.Attribute("Id") ?? "",
                    Version = (string?)targetEl.Attribute("Version") ?? "[1.0,)"
                });
            }
        }

        // Parse Dependencies
        var dependenciesEl = root.Element(ns + "Dependencies") ?? root.Element("Dependencies");
        var dependencies = new List<ManifestDependency>();
        if (dependenciesEl != null)
        {
            foreach (var depEl in dependenciesEl.Elements(ns + "Dependency").Concat(dependenciesEl.Elements("Dependency")))
            {
                dependencies.Add(new ManifestDependency
                {
                    Id = (string?)depEl.Attribute("Id") ?? "",
                    DisplayName = (string?)depEl.Attribute("DisplayName"),
                    Version = (string?)depEl.Attribute("Version") ?? "[1.0,)"
                });
            }
        }

        // Parse Assets
        var assetsEl = root.Element(ns + "Assets") ?? root.Element("Assets");
        var assets = new List<ManifestAsset>();
        if (assetsEl != null)
        {
            foreach (var assetEl in assetsEl.Elements(ns + "Asset").Concat(assetsEl.Elements("Asset")))
            {
                var assetType = (string?)assetEl.Attribute("Type") ?? "";
                assets.Add(new ManifestAsset
                {
                    Type = assetType,
                    Path = (string?)assetEl.Attribute("Path"),
                    TargetHost = (string?)assetEl.Attribute("TargetHost")
                });
            }
        }

        return new VsixManifest
        {
            Version = version,
            Metadata = metadata,
            Installation = installationTargets,
            Dependencies = dependencies,
            Assets = assets
        };
    }

    private static string? GetElementValue(XElement parent, XNamespace ns, string localName)
    {
        var el = parent.Element(ns + localName) ?? parent.Element(localName);
        return el?.Value;
    }
}

