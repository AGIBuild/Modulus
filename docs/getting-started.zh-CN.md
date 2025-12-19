# Modulus 快速入门

本指南将引导您完成开发环境的搭建、创建第一个模块，并在 Modulus 主机中运行它。

## 环境要求

- **.NET 9 SDK** 或更高版本
- **IDE**: Visual Studio 2022、JetBrains Rider 或 VS Code
- **Git**（可选，用于克隆仓库）

## 安装

### 方式一：从 NuGet 安装 CLI（推荐）

```bash
# 全局安装 Modulus CLI
dotnet tool install -g Agibuild.Modulus.Cli

# 验证安装
modulus --help
```

### 方式二：安装项目模板

```bash
# 安装模块项目模板
dotnet new install Agibuild.Modulus.Templates

# 验证模板已安装
dotnet new list modulus
```

您应该看到：

```
模板名                        短名称             语言     标签
---------------------------  ----------------  --------  --------------------------------
Modulus Module (Avalonia)    modulus-avalonia  [C#]      Modulus/Module/Plugin/Avalonia
Modulus Module (Blazor)      modulus-blazor    [C#]      Modulus/Module/Plugin/Blazor
```

### 方式三：从源码构建

```bash
# 克隆仓库
git clone https://github.com/AGIBuild/Modulus.git
cd Modulus

# 构建解决方案
dotnet build

# 运行 Avalonia 主机
dotnet run --project src/Hosts/Modulus.Host.Avalonia
```

## 快速开始：创建您的第一个模块

### 第一步：创建新模块

使用 CLI：

```bash
modulus new -n MyFirstModule
cd MyFirstModule
```

或使用 dotnet new：

```bash
dotnet new modulus-avalonia -n MyFirstModule
cd MyFirstModule
```

这将创建以下结构的模块：

```
MyFirstModule/
├── MyFirstModule.sln              # 解决方案文件
├── .gitignore
├── MyFirstModule.Core/            # 核心逻辑（主机无关）
│   ├── MyFirstModule.Core.csproj
│   ├── MyFirstModuleModule.cs     # 模块入口点
│   └── ViewModels/
│       └── MainViewModel.cs       # 主 ViewModel
└── MyFirstModule.UI.Avalonia/     # Avalonia UI
    ├── MyFirstModule.UI.Avalonia.csproj
    ├── MyFirstModuleAvaloniaModule.cs
    ├── MainView.axaml             # UI 定义
    └── MainView.axaml.cs
```

### 第二步：编译模块

```bash
modulus build
```

或：

```bash
dotnet build
```

### 第三步：打包分发

```bash
modulus pack
```

这将在 `./output/` 目录下创建 `.modpkg` 文件。

### 第四步：安装模块

```bash
modulus install ./output/MyFirstModule-1.0.0.modpkg
```

### 第五步：运行主机

```bash
# 如果已安装 Modulus 主机
modulus-host

# 或从 Modulus 源码运行
dotnet run --project path/to/Modulus/src/Hosts/Modulus.Host.Avalonia
```

您的模块将出现在侧边栏中！

## 模块开发工作流

```
┌─────────────┐     ┌─────────────┐     ┌─────────────┐
│    创建     │────▶│    编译     │────▶│    打包     │
│ modulus new │     │modulus build│     │ modulus pack│
└─────────────┘     └─────────────┘     └─────────────┘
                                               │
                                               ▼
┌─────────────┐     ┌─────────────┐     ┌─────────────┐
│    运行     │◀────│    安装     │◀────│    输出     │
│  主机应用   │     │modulus inst.│     │  .modpkg    │
└─────────────┘     └─────────────┘     └─────────────┘
```

## 理解模块结构

### Core 项目 (`*.Core`)

包含业务逻辑、ViewModels 和服务。此代码是**主机无关的**，可以在 Avalonia 和 Blazor 主机上运行。

关键文件：

- **`*Module.cs`**：模块入口点，继承自 `ModulusPackage`
- **`ViewModels/`**：模块的 MVVM ViewModels
- **`Services/`**：业务逻辑服务（您自行添加）

### UI 项目 (`*.UI.Avalonia` / `*.UI.Blazor`)

包含每个主机平台的 UI 实现。

- **Avalonia**：使用 `.axaml` 文件进行基于 XAML 的 UI
- **Blazor**：使用 `.razor` 文件进行基于组件的 UI

### 模块清单 (`extension.vsixmanifest`)

清单文件由模板自动生成，包含：

```xml
<PackageManifest Version="2.0.0">
  <Metadata>
    <Identity Id="unique-guid" Version="1.0.0" Publisher="YourName" />
    <DisplayName>MyFirstModule</DisplayName>
    <Description>模块描述</Description>
  </Metadata>
  <Assets>
    <!-- 模块程序集 -->
    <Asset Type="Modulus.Package" Path="MyFirstModule.Core.dll" />

    <!-- Host-specific UI packages -->
    <Asset Type="Modulus.Package" Path="MyFirstModule.UI.Avalonia.dll" TargetHost="Modulus.Host.Avalonia" />
    <Asset Type="Modulus.Package" Path="MyFirstModule.UI.Blazor.dll" TargetHost="Modulus.Host.Blazor" />
    
    <!-- 菜单不再在清单中声明。
         菜单通过 host-specific 模块入口类型上的 [AvaloniaMenu]/[BlazorMenu] 声明，
         并在安装/启动同步时投影到数据库。 -->
  </Assets>
</PackageManifest>
```

## CLI 命令参考

| 命令 | 描述 |
|------|------|
| `modulus new <name>` | 创建新模块项目 |
| `modulus build` | 在当前目录编译模块 |
| `modulus pack` | 编译并打包为 .modpkg |
| `modulus install <source>` | 从 .modpkg 或目录安装模块 |
| `modulus uninstall <name>` | 卸载模块 |
| `modulus list` | 列出已安装模块 |

### modulus new

```bash
modulus new MyModule [options]

选项:
  -t, --target <avalonia|blazor>  目标主机平台
  -d, --display-name <name>       菜单中显示的名称
  -p, --publisher <name>          发布者名称
  -i, --icon <icon>               菜单图标（如 Folder, Home, Settings）
  --output <path>                 输出目录
  --force                         覆盖已有文件
```

### modulus build

```bash
modulus build [options]

选项:
  -p, --path <path>               模块项目路径
  -c, --configuration <config>    构建配置（默认: Release）
  -v, --verbose                   显示详细输出
```

### modulus pack

```bash
modulus pack [options]

选项:
  -p, --path <path>               模块项目路径
  -o, --output <path>             输出目录（默认: ./output）
  -c, --configuration <config>    构建配置（默认: Release）
  --no-build                      跳过编译，使用已有输出
  -v, --verbose                   显示详细输出
```

### modulus install

```bash
modulus install <source> [options]

参数:
  <source>    .modpkg 文件或模块目录路径

选项:
  -f, --force    覆盖已有安装
  -v, --verbose  显示详细输出
```

## 下一步

- [模块开发指南](./module-development.zh-CN.md) - 深入了解模块开发
- [CLI 参考](./cli-reference.zh-CN.md) - 完整的 CLI 命令参考
- [清单格式](./manifest-format.zh-CN.md) - 理解 extension.vsixmanifest
- [UI 组件](./ui-components.zh-CN.md) - 可用的 UI 组件和样式

## 故障排除

### 安装后找不到模块

重启 Modulus 主机应用程序。模块在启动时加载。

### NuGet 包构建错误

确保您有最新的 Modulus SDK 包：

```bash
dotnet restore
```

### 模块未出现在菜单中

检查 host-specific 模块入口类型是否声明了菜单属性：

- Avalonia：`[AvaloniaMenu("key", "Display", typeof(MainViewModel), ...)]`
- Blazor：`[BlazorMenu("key", "Display", "/route", ...)]`

导航菜单渲染时只读取数据库。如从旧版本升级，请删除 Host 的数据库文件后重启。

## 获取帮助

- GitHub Issues: [报告 bug 或请求功能](https://github.com/AGIBuild/Modulus/issues)
- Discussions: [提问](https://github.com/AGIBuild/Modulus/discussions)

