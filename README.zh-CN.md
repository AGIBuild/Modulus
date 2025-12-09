# Modulus

Modulus 是一个现代化的跨平台插件式应用框架，帮助开发者快速构建可扩展、可维护、支持 AI 的工具类软件。

## ✨ 特性亮点

### 多主机架构
- **UI 无关核心**: 业务逻辑独立于任何 UI 框架
- **可插拔主机**: 支持 Avalonia (桌面) 和 Blazor Hybrid (MAUI)
- **共享核心逻辑**: 相同的 Domain/Application 代码运行在所有主机上

### 扩展系统
- **VS Extension 兼容**: 使用 `extension.vsixmanifest` (XML) 格式
- **热重载**: 基于 AssemblyLoadContext 的隔离，支持动态加载/卸载
- **显式安装**: 通过 CLI 或 UI 安装扩展，不自动扫描目录
- **类型安全入口点**: `ModulusPackage` 基类，类似 VS VsPackage

### 开发体验
- 扩展 SDK，支持声明式属性
- AI Agent 插件支持（可嵌入 LLM）
- 签名验证与版本控制
- 跨平台: Windows / macOS / Linux

## 🏗️ 架构

```
src/
├── Modulus.Core/              # 运行时、模块加载器、DI
├── Modulus.Sdk/               # SDK: ModulusPackage, 属性
├── Modulus.UI.Abstractions/   # UI 契约 (IMenuRegistry, INavigationService)
├── Hosts/
│   ├── Modulus.Host.Avalonia/ # Avalonia 桌面 (ID: Modulus.Host.Avalonia)
│   └── Modulus.Host.Blazor/   # Blazor Hybrid (ID: Modulus.Host.Blazor)
└── Modules/
    ├── EchoPlugin/            # 示例: Echo 插件
    ├── SimpleNotes/           # 示例: 笔记模块
    └── ComponentsDemo/        # 示例: UI 组件演示
```

## 📦 扩展结构

```
MyExtension/
├── extension.vsixmanifest     # XML 清单 (VS Extension 格式)
├── MyExtension.Core.dll       # 核心逻辑 (host-agnostic)
├── MyExtension.UI.Avalonia.dll
└── MyExtension.UI.Blazor.dll
```

## 🚀 快速开始

### 运行 Avalonia 主机
```bash
dotnet run --project src/Hosts/Modulus.Host.Avalonia
```

### 运行 Blazor 主机
```bash
dotnet run --project src/Hosts/Modulus.Host.Blazor
```

### 运行测试
```bash
dotnet test
```

## 🔌 创建扩展

### 1. 创建项目

```
MyExtension/
├── MyExtension.Core/
├── MyExtension.UI.Avalonia/
└── MyExtension.UI.Blazor/
```

### 2. 定义入口点

```csharp
// MyExtension.Core/MyExtensionPackage.cs
public class MyExtensionPackage : ModulusPackage
{
    public override void ConfigureServices(IModuleLifecycleContext context)
    {
        context.Services.AddSingleton<IMyService, MyService>();
    }
}
```

### 3. 创建清单

```xml
<!-- extension.vsixmanifest -->
<?xml version="1.0" encoding="utf-8"?>
<PackageManifest Version="2.0.0" 
    xmlns="http://schemas.microsoft.com/developer/vsx-schema/2011">
  <Metadata>
    <Identity Id="your-guid" Version="1.0.0" Publisher="You" />
    <DisplayName>My Extension</DisplayName>
    <Description>My awesome extension</Description>
  </Metadata>
  <Installation>
    <InstallationTarget Id="Modulus.Host.Avalonia" Version="[1.0,)" />
    <InstallationTarget Id="Modulus.Host.Blazor" Version="[1.0,)" />
  </Installation>
  <Assets>
    <Asset Type="Modulus.Package" Path="MyExtension.Core.dll" />
    <Asset Type="Modulus.Package" Path="MyExtension.UI.Avalonia.dll" 
           TargetHost="Modulus.Host.Avalonia" />
    <Asset Type="Modulus.Menu" Id="my-menu" DisplayName="My Tool" 
           Icon="Home" Route="MyExtension.ViewModels.MainViewModel" 
           TargetHost="Modulus.Host.Avalonia" />
  </Assets>
</PackageManifest>
```

### 4. 安装扩展

```bash
modulus install ./MyExtension
```

## 📚 文档

- [OpenSpec 规格说明](./openspec/specs/)
- [项目上下文](./openspec/project.md)
- [贡献指南](./CONTRIBUTING.zh-CN.md)

## 项目状态

- **阶段**: 活跃开发中
- **测试覆盖**: 30+ 测试通过
- **平台**: Windows, macOS, Linux

## 贡献

欢迎提交 Issue 和 PR！请参阅 [CONTRIBUTING.zh-CN.md](./CONTRIBUTING.zh-CN.md)。

## 许可证

[MIT License](./LICENSE)
