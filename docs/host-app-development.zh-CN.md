# Host App 开发指南

本文档说明如何通过 **CLI** 或 **Visual Studio 模板** 创建并定制 **Modulus Host App（插件式应用）**。

## Host App 是什么？

- **Host App**：可执行应用，用于安装/加载/运行 Modulus 模块。
- **Module**：可独立构建与打包的插件（`.modpkg`），由 Host 安装并加载。

## 创建 Host App

### 方式 1：CLI（推荐）

```bash
# Avalonia 桌面 Host App
modulus new avaloniaapp -n MyApp

# Blazor Hybrid（MAUI）Host App
modulus new blazorapp -n MyApp
```

### 方式 2：Visual Studio 模板（向导）

请参考：[`templates/VSIX/README.md`](../templates/VSIX/README.md)

## 生成结构

```
MyApp/
├── MyApp.sln
├── .gitignore
├── Directory.Build.props
├── appsettings.json
└── MyApp.Host.Avalonia/        # 或：MyApp.Host.Blazor
    └── ... host 项目文件 ...
```

## 构建与运行

### Avalonia（`avaloniaapp`）

```bash
cd MyApp
dotnet build -c Release
dotnet run --project MyApp.Host.Avalonia -c Release
```

### Blazor Hybrid（`blazorapp`）

`blazorapp` 是 **.NET MAUI** Host 模板，通常需要 **Windows** 才能稳定构建。

```bash
dotnet workload install maui
```

```bash
cd MyApp
dotnet build -c Release
dotnet run --project MyApp.Host.Blazor -c Release
```

## 关键配置（`appsettings.json`）

Host 模板使用：

- `Modulus:Runtime:SharedAssemblies`（精确的程序集简单名列表）
- `Modulus:Runtime:SharedAssemblyPrefixes`（前缀规则，用于框架族）

建议：

- Modulus/Host SDK 相关优先用 **精确名称**
- UI 框架相关用 **前缀**（例如 Avalonia、MAUI、MudBlazor）
- 避免过宽前缀（会误把不该共享的程序集当作共享）

## 模块目录（Host 从哪里加载模块）

模板默认调用 `AddDefaultModuleDirectories()`，会加入：

- 系统模块：`{AppBaseDir}/Modules`
- 用户模块（Windows）：`%APPDATA%/Modulus/Modules`
- 用户模块（macOS/Linux）：`~/.modulus/Modules`

CLI 默认安装路径为：

- Windows：`%APPDATA%/Modulus/Modules`
- macOS/Linux：`~/.modulus/Modules`

默认 Host App 模板已经与 CLI 安装目录对齐。
如果你在自定义 Host 中需要显式加入该目录，可以在 Host 代码里添加：

```csharp
using Modulus.Core.Paths;

// Add ~/.modulus/Modules so the host can load modules installed by the Modulus CLI on macOS/Linux.
sdkBuilder.AddModuleDirectory(
    path: Path.Combine(LocalStorage.GetUserRoot(), "Modules"),
    isSystem: false);
```

（对应英文说明：[`docs/host-app-development.md`](./host-app-development.md)）


