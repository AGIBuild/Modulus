# Quickstart: Modulus 模块开发指南

**Feature**: `001-core-architecture`  
**Updated**: 2025-12-03

本文档帮助开发者快速上手 Modulus 模块开发。

---

## 1. 项目结构概览

```text
src/
├── Modulus.Core/                 # 核心运行时 (RuntimeContext, ModuleLoader, ModuleManager)
├── Modulus.Sdk/                  # SDK 基类与属性 (ModuleBase, ModuleAttribute, etc.)
├── Modulus.UI.Abstractions/      # UI 抽象接口 (IMenuRegistry, IThemeService, etc.)
├── Hosts/
│   ├── Modulus.Host.Blazor/      # Blazor Hybrid 宿主 (MAUI + MudBlazor)
│   └── Modulus.Host.Avalonia/    # Avalonia 桌面宿主
└── Modules/
    ├── EchoPlugin/               # 示例: Echo 插件
    │   ├── EchoPlugin.Core/
    │   ├── EchoPlugin.UI.Avalonia/
    │   └── EchoPlugin.UI.Blazor/
    └── SimpleNotes/              # 示例: 笔记模块
        ├── SimpleNotes.Core/
        ├── SimpleNotes.UI.Avalonia/
        └── SimpleNotes.UI.Blazor/
```

---

## 2. 创建新模块

### 2.1 项目结构

每个模块由三个项目组成：

| 项目 | 类型 | 引用 |
|------|------|------|
| `MyModule.Core` | Class Library | `Modulus.Sdk`, `Modulus.UI.Abstractions` |
| `MyModule.UI.Avalonia` | Class Library | `MyModule.Core`, `Avalonia` |
| `MyModule.UI.Blazor` | Razor Class Library | `MyModule.Core`, `MudBlazor` |

### 2.2 Core 模块类

```csharp
using Modulus.Sdk;
using Modulus.Sdk.Attributes;

namespace MyModule.Core;

[Module(
    Id = "my-module-guid-here",
    DisplayName = "My Module",
    Description = "A sample module")]
public class MyModuleModule : ModuleBase
{
    public override void ConfigureServices(IModuleLifecycleContext context)
    {
        // Register services
        context.Services.AddTransient<MyViewModel>();
    }
}
```

### 2.3 ViewModel (使用 CommunityToolkit.Mvvm)

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MyModule.Core.ViewModels;

public partial class MyViewModel : ObservableObject
{
    [ObservableProperty]
    private string _title = "My Module";

    [ObservableProperty]
    private string _inputText = string.Empty;

    [RelayCommand]
    private void DoSomething()
    {
        // Business logic here
    }
}
```

### 2.4 Avalonia UI 模块

```csharp
using Modulus.Sdk;
using Modulus.Sdk.Attributes;

namespace MyModule.UI.Avalonia;

[DependsOn(typeof(MyModuleModule))]
[AvaloniaMenu(
    DisplayName = "My Module",
    Icon = "🔧",
    ViewModelType = typeof(MyViewModel),
    Location = MenuLocation.Main,
    Order = 50)]
public class MyModuleAvaloniaModule : ModuleBase
{
    public override Task OnApplicationInitializationAsync(
        IModuleInitializationContext context, 
        CancellationToken cancellationToken = default)
    {
        var viewRegistry = context.ServiceProvider.GetRequiredService<IViewRegistry>();
        viewRegistry.Register<MyViewModel, MyView>();
        return Task.CompletedTask;
    }
}
```

### 2.5 Blazor UI 模块

```csharp
using Modulus.Sdk;
using Modulus.Sdk.Attributes;

namespace MyModule.UI.Blazor;

[DependsOn(typeof(MyModuleModule))]
[BlazorMenu(
    DisplayName = "My Module",
    Icon = "extension",  // MudBlazor icon name
    Route = "/mymodule",
    Location = MenuLocation.Main,
    Order = 50)]
public class MyModuleBlazorModule : ModuleBase
{
    // Blazor uses route-based navigation, no view registration needed
}
```

---

## 3. Manifest 配置

每个模块需要一个 `manifest.json` 文件：

```json
{
  "manifestVersion": "1.0",
  "id": "a1b2c3d4-e5f6-4a7b-8c9d-0e1f2a3b4c5d",
  "version": "1.0.0",
  "displayName": "My Module",
  "description": "A sample module for demonstration.",
  "supportedHosts": ["BlazorApp", "AvaloniaApp"],
  "coreAssemblies": ["MyModule.Core.dll"],
  "uiAssemblies": {
    "BlazorApp": ["MyModule.UI.Blazor.dll"],
    "AvaloniaApp": ["MyModule.UI.Avalonia.dll"]
  },
  "dependencies": {}
}
```

**重要**: 
- `id` 推荐使用 GUID 以确保唯一性
- `manifest.json` 需要复制到输出目录（在 `.csproj` 中配置）

```xml
<ItemGroup>
  <None Include="..\manifest.json" CopyToOutputDirectory="PreserveNewest" Link="manifest.json" />
</ItemGroup>
```

---

## 4. 模块生命周期

模块生命周期方法按以下顺序调用：

1. **ConfigureServices** - 注册 DI 服务
2. **PreConfigureAsync** - 预配置（依赖模块之前）
3. **ConfigureAsync** - 主配置
4. **PostConfigureAsync** - 后配置（依赖模块之后）
5. **OnApplicationInitializationAsync** - 应用初始化（注册视图、菜单等）
6. **OnApplicationShutdownAsync** - 应用关闭时清理

```csharp
public class MyModuleModule : ModuleBase
{
    public override void ConfigureServices(IModuleLifecycleContext context)
    {
        // Step 1: Register services
    }

    public override Task OnApplicationInitializationAsync(
        IModuleInitializationContext context, 
        CancellationToken cancellationToken = default)
    {
        // Step 5: Register menus, views, etc.
        return Task.CompletedTask;
    }

    public override Task OnApplicationShutdownAsync(
        IModuleInitializationContext context, 
        CancellationToken cancellationToken = default)
    {
        // Step 6: Cleanup
        return Task.CompletedTask;
    }
}
```

---

## 5. 依赖管理

使用 `[DependsOn]` 属性声明模块依赖：

```csharp
[DependsOn(typeof(CoreModule), typeof(LoggingModule))]
public class MyModuleModule : ModuleBase
{
    // This module will be initialized after CoreModule and LoggingModule
}
```

---

## 6. 宿主类型

Modulus 支持两种宿主类型：

| 宿主 | 标识符 | UI 框架 |
|------|--------|---------|
| Blazor Hybrid | `BlazorApp` | MAUI + MudBlazor |
| Avalonia | `AvaloniaApp` | Avalonia UI |

模块可以通过 `RuntimeContext.HostType` 获取当前宿主类型。

---

## 7. UI 抽象接口

### IMenuRegistry
注册导航菜单项：

```csharp
var menuRegistry = context.ServiceProvider.GetRequiredService<IMenuRegistry>();
menuRegistry.Register(new MenuItem(
    id: "my-menu",
    displayName: "My Module",
    icon: "🔧",
    navigationKey: typeof(MyViewModel).FullName!,
    location: MenuLocation.Main,
    order: 50));
```

### IThemeService
管理应用主题：

```csharp
var themeService = context.ServiceProvider.GetRequiredService<IThemeService>();
themeService.SetTheme(AppTheme.Dark);
```

### INotificationService
显示通知：

```csharp
var notificationService = context.ServiceProvider.GetRequiredService<INotificationService>();
await notificationService.ShowInfoAsync("Title", "Message");
```

---

## 8. 数据持久化

Modulus 使用 SQLite + EF Core 存储应用设置和模块状态：

### ISettingsService
存取应用设置：

```csharp
var settings = context.ServiceProvider.GetRequiredService<ISettingsService>();

// Get setting with default value
var theme = settings.GetSetting("AppTheme", AppTheme.System);

// Set setting
settings.SetSetting("AppTheme", AppTheme.Dark);
```

---

## 9. 运行与调试

### 启动 Avalonia 宿主
```bash
dotnet run --project src/Hosts/Modulus.Host.Avalonia
```

### 启动 Blazor 宿主
```bash
dotnet run --project src/Hosts/Modulus.Host.Blazor
```

### 运行测试
```bash
dotnet test
```

---

## 10. 最佳实践

1. **保持 Core 模块 UI 无关** - 不要在 Core 项目中引用任何 UI 框架
2. **使用 GUID 作为模块 ID** - 确保模块标识的唯一性
3. **使用声明式属性** - 优先使用 `[Module]`, `[AvaloniaMenu]`, `[BlazorMenu]` 属性
4. **遵循 MVVM 模式** - 使用 CommunityToolkit.Mvvm 实现 ViewModel
5. **正确配置 manifest.json** - 确保复制到输出目录
6. **测试驱动** - 为模块编写单元测试和集成测试
