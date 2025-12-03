using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace Modulus.Sdk;

public class PluginPackageBuilder
{
    private string _id;
    private string _version;
    private string? _displayName;
    private string? _description;
    private readonly List<string> _supportedHosts = new();
    private readonly List<string> _coreAssemblies = new();
    private readonly Dictionary<string, List<string>> _uiAssemblies = new();
    private readonly Dictionary<string, string> _dependencies = new();

    public PluginPackageBuilder(string id, string version)
    {
        _id = id;
        _version = version;
    }

    public PluginPackageBuilder WithDisplayName(string displayName)
    {
        _displayName = displayName;
        return this;
    }

    public PluginPackageBuilder WithDescription(string description)
    {
        _description = description;
        return this;
    }

    public PluginPackageBuilder AddHost(string hostType)
    {
        if (!_supportedHosts.Contains(hostType))
        {
            _supportedHosts.Add(hostType);
        }
        return this;
    }

    public PluginPackageBuilder AddCoreAssembly(string assemblyName)
    {
        if (!_coreAssemblies.Contains(assemblyName))
        {
            _coreAssemblies.Add(assemblyName);
        }
        return this;
    }

    public PluginPackageBuilder AddUiAssembly(string hostType, string assemblyName)
    {
        AddHost(hostType);
        
        if (!_uiAssemblies.TryGetValue(hostType, out var list))
        {
            list = new List<string>();
            _uiAssemblies[hostType] = list;
        }

        if (!list.Contains(assemblyName))
        {
            list.Add(assemblyName);
        }
        return this;
    }

    public PluginPackageBuilder AddDependency(string moduleId, string version)
    {
        _dependencies[moduleId] = version;
        return this;
    }

    public ModuleManifest Build()
    {
        return new ModuleManifest
        {
            Id = _id,
            Version = _version,
            DisplayName = _displayName,
            Description = _description,
            SupportedHosts = new List<string>(_supportedHosts),
            CoreAssemblies = new List<string>(_coreAssemblies),
            UiAssemblies = new Dictionary<string, List<string>>(_uiAssemblies),
            Dependencies = new Dictionary<string, string>(_dependencies)
        };
    }

    public async Task SaveManifestAsync(string directory)
    {
        var manifest = Build();
        var path = Path.Combine(directory, "manifest.json");
        var options = new JsonSerializerOptions { WriteIndented = true };
        
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        using var stream = File.Create(path);
        await JsonSerializer.SerializeAsync(stream, manifest, options);
    }
}
