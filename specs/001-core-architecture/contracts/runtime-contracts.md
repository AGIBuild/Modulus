# Runtime Contracts & Interfaces

## Module System

### IModule
所有模块必须实现的接口。推荐继承 `Modulus.Sdk.ModuleBase`。

```csharp
public interface IModule
{
    void PreConfigureServices(IModuleLifecycleContext context);
    void ConfigureServices(IModuleLifecycleContext context);
    void PostConfigureServices(IModuleLifecycleContext context);

    Task OnApplicationInitializationAsync(IModuleInitializationContext context, CancellationToken cancellationToken = default);
    Task OnApplicationShutdownAsync(IModuleInitializationContext context, CancellationToken cancellationToken = default);
}
```

### IModuleProvider
定义模块发现策略。

```csharp
public interface IModuleProvider
{
    Task<IEnumerable<string>> GetModulePackagesAsync(CancellationToken cancellationToken = default);
    bool IsSystemSource { get; }
}
```

### IModuleLoader
负责加载、卸载和重载模块。

```csharp
public interface IModuleLoader
{
    Task<ModuleDescriptor?> LoadAsync(string packagePath, bool isSystem = false, CancellationToken cancellationToken = default);
    Task UnloadAsync(string moduleId);
    Task<ModuleDescriptor?> ReloadAsync(string moduleId, CancellationToken cancellationToken = default);
    Task<ModuleDescriptor?> GetDescriptorAsync(string packagePath, CancellationToken cancellationToken = default);
}
```

## Module Manifest
`manifest.json` 结构定义 (see `Modulus.Sdk.ModuleManifest`).

```json
{
  "id": "string",
  "version": "string",
  "displayName": "string?",
  "description": "string?",
  "supportedHosts": ["string"],
  "coreAssemblies": ["string"],
  "uiAssemblies": {
    "HostType": ["string"]
  },
  "dependencies": {
    "ModuleId": "Version"
  }
}
```

## SDK Helpers
`Modulus.Sdk.PluginPackageBuilder` 用于辅助构建插件包结构和清单。
