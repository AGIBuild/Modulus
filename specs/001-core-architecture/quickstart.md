## 6. 使用 SDK 创建第一个插件 (User Story 3)

Modulus SDK 提供了简化的开发体验。推荐使用 AI 辅助生成插件。

### 6.1 基本结构
一个标准的 SDK 插件包含：
1. **Core 项目** (Class Library): 引用 `Modulus.Sdk` 和 `Modulus.UI.Abstractions`。包含 ViewModels 和 Module 类。
2. **UI.Blazor 项目** (Razor Class Library): 引用 Core 和 `MudBlazor`。包含 Razor 组件。
3. **UI.Avalonia 项目** (Class Library): 引用 Core 和 `Avalonia`。包含 UserControls。

### 6.2 示例代码模板 (EchoPlugin)

**ViewModel (Core):**
```csharp
public partial class EchoViewModel : ViewModelBase
{
    [ObservableProperty] private string _inputText;
    
    [RelayCommand]
    private void Echo() => /* logic */
}
```

**Module Registration (Core):**
```csharp
public class EchoPluginModule : ModuleBase
{
    public override void ConfigureServices(IModuleLifecycleContext context)
    {
        context.Services.AddTransient<EchoViewModel>();
    }
}
```

**UI Registration (Host-Specific Module):**
```csharp
public class EchoPluginBlazorModule : ModuleBase
{
    public override Task OnApplicationInitializationAsync(IModuleInitializationContext context, CancellationToken cancellationToken = default)
    {
        var viewRegistry = context.ServiceProvider.GetRequiredService<IViewRegistry>();
        viewRegistry.Register<EchoViewModel, EchoView>(); // EchoView is Razor Component
        return Task.CompletedTask;
    }
}
```

### 6.3 打包与清单
使用 `manifest.json` 描述插件：
```json
{
  "id": "MyPlugin",
  "version": "1.0.0",
  "supportedHosts": ["BlazorApp", "AvaloniaApp"],
  "coreAssemblies": ["MyPlugin.Core.dll"],
  "uiAssemblies": {
    "BlazorApp": ["MyPlugin.UI.Blazor.dll"],
    "AvaloniaApp": ["MyPlugin.UI.Avalonia.dll"]
  }
}
```
使用 `PluginPackageBuilder` (SDK) 可以在代码中生成此清单。
