# Solution Design: Plugin Contract and Foundation (Story 004)

## Overview
This document summarizes the recommended implementation order, rationale, and testing strategies for Story 004: "Define a unified plugin contract and foundation (DI, config, localization, logging) for safe, consistent plugin integration."

## Recommended Task Order

1. **Create Modulus.Plugin.Abstractions Project**
   - Define interfaces:
     - `IPlugin`: Main plugin contract, including metadata, DI, initialization, and UI extension points.
     - `IPluginMeta`: Metadata contract (Name, Version, Description, Author, Dependencies).
     - `ILocalizer`: Localization contract (resource access, language switching).
     - (Optional) `IPluginSettings`: For plugin-specific configuration.
   - Example interface definitions:

```csharp
public interface IPlugin
{
    IPluginMeta Meta { get; }
    void ConfigureServices(IServiceCollection services, IConfiguration configuration);
    void Initialize(IServiceProvider provider);
    object? GetMainView(); // For UI plugins, e.g., Avalonia Control
    object? GetMenu();     // For menu extension
}

public interface IPluginMeta
{
    string Name { get; }
    string Version { get; }
    string Description { get; }
    string Author { get; }
    string[]? Dependencies { get; }
    string ContractVersion { get; } // Required: plugin contract version
}

public interface ILocalizer
{
    string this[string key] { get; }
    string CurrentLanguage { get; }
    void SetLanguage(string lang);
    IEnumerable<string> SupportedLanguages { get; }
}

public interface IPluginSettings
{
    IConfiguration Configuration { get; }
}
```

2. **Update Plugin Template and Main App to Reference SDK**
   - Add project/package reference to `Modulus.Plugin.Abstractions` in both plugin template and main app.
   - Ensure all plugin entry points implement `IPlugin`.

3. **Standardize Plugin Directory Structure and Metadata**
   - Each plugin in its own subdirectory:
     - `PluginA/PluginA.dll`
     - `PluginA/pluginsettings.json`
     - `PluginA/lang.en.json`, `PluginA/lang.zh.json`, ...
   - Example `pluginsettings.json`:
```json
{
  "ContractVersion": "2.0.0",
  "SettingA": "value",
  "SettingB": 123
}
```
   - Example `lang.en.json`:
```json
{
  "Hello": "Hello",
  "Exit": "Exit"
}
```

4. **Upgrade PluginLoader to Only Load IPlugin Implementations**
   - Use reflection to ensure only assemblies with `IPlugin` implementations are loaded.
   - Example:
```csharp
var asm = context.LoadFromAssemblyPath(pluginPath);
var pluginType = asm.GetTypes().FirstOrDefault(t => typeof(IPlugin).IsAssignableFrom(t) && !t.IsAbstract);
if (pluginType == null) throw new InvalidOperationException("No valid IPlugin implementation found.");
```

5. **Implement DI, Config, and Logging Support**
   - `IPlugin.ConfigureServices` for service registration.
   - `IPlugin.Initialize` for runtime setup with `IServiceProvider`.
   - Plugins access `IConfiguration` and `ILogger<T>` via DI.
   - Example usage in plugin:
```csharp
public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
{
    services.AddSingleton<IMyService, MyService>();
}
public void Initialize(IServiceProvider provider)
{
    var logger = provider.GetRequiredService<ILogger<MyPlugin>>();
    logger.LogInformation("Plugin initialized");
}
```

6. **Documentation and Example Plugins**
   - Write English interface docs and sample plugin code.
   - Example README snippet for plugin authors:
```markdown
## Plugin Contract
- Implement `IPlugin` from Modulus.Plugin.Abstractions
- Provide metadata, DI, and (optionally) UI extension points
- Place your plugin DLL and config/localization files in a dedicated subdirectory
```

## Version Compatibility and Error Handling

### Problem
When the plugin contract (SDK) version changes, there are two main incompatibility scenarios:
1. **Plugin is too old**: The plugin's contract version is lower than the minimum required by the host application.
2. **Host is too old**: The plugin requires a newer contract version than the host supports.

### Solution
- **Contract Version Declaration**: Both the host and each plugin declare their supported/required contract version (e.g., `ContractVersion` in `IPluginMeta` and a constant in the host).
- **Compatibility Check**: When loading a plugin, the host compares its supported contract version with the plugin's declared version.
- **Error Handling**:
  - If the plugin is too old (plugin version < host minimum):
    - Do not load the plugin.
    - Show/log a user-friendly message: `The plugin "{Name}" is not compatible with this version of Modulus. Please contact the plugin developer to update the plugin.`
  - If the host is too old (plugin version > host maximum):
    - Do not load the plugin.
    - Show/log a user-friendly message: `The plugin "{Name}" requires a newer version of Modulus. Please update the application to use this plugin.`
  - For all other contract mismatches, provide a clear error message and do not throw raw exceptions to the user.

#### Example Implementation
```csharp
const string HostContractVersion = "2.0.0";
const string HostMinSupportedVersion = "1.0.0";

public object? RunPluginWithContractCheck(string pluginPath)
{
    var meta = ReadMeta(pluginPath);
    if (meta == null)
        throw new InvalidOperationException("Plugin metadata not found.");

    Version pluginVer = new Version(meta.ContractVersion);
    Version hostVer = new Version(HostContractVersion);
    Version hostMin = new Version(HostMinSupportedVersion);

    if (pluginVer < hostMin)
        throw new PluginContractException($"The plugin '{meta.Name}' is too old and not compatible with this version of Modulus. Please contact the plugin developer to update the plugin.");
    if (pluginVer > hostVer)
        throw new PluginContractException($"The plugin '{meta.Name}' requires a newer version of Modulus. Please update the application to use this plugin.");

    // ...existing plugin loading logic...
}
```

- **UI/Log Integration**: Catch `PluginContractException` and display/log the message to the user, not a raw stack trace.
- **Documentation**: Clearly document the contract versioning policy for both plugin and host developers.

## Testing Strategy
- **Contract Tests**: Use reflection/mocks to ensure only `IPlugin` implementations are loaded.
- **DI/Config/Logging**: Integration tests to verify plugins can register/resolve services, read config, and log.
- **Localization**: Test plugins can load and switch language resources via `ILocalizer`.
- **End-to-End**: Load multiple plugins, verify isolation, config, and logging.

## File/Project Organization (Recommended)
- `src/Modulus.Plugin.Abstractions/` (new project, all contracts/interfaces)
- `src/Modulus.PluginHost/` (host, references Abstractions)
- `src/Modulus.App/` (main app, references Abstractions)
- `tools/modulus-plugin/` (plugin template, references Abstractions)
- Each plugin: its own subdirectory with `dll`, `pluginsettings.json`, `lang.xx.json`, etc.

## Notes
- All code comments and README files must be in English.
- Story/requirement docs can remain in Chinese for team communication.
- This solution is flexible: teams can adjust file/project layout as long as contract and testability are preserved.

---

**This document is intended as a reference for all team members implementing or extending the plugin system foundation.**
