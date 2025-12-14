# Change: 全量引入项目前缀（Agibuild.Modulus）并重命名 Host Id / Solution / Project / Assembly

## Why

当前仓库存在多种“标识符层级”混用（Solution/Project/Assembly/PackageId/Host Id），在发布 NuGet、分发模板、以及模块清单 `InstallationTarget` 校验时容易产生冲突与歧义。

为了保证可发布性、识别度与一致性，我们需要将所有一方产物统一加上 **公司+产品前缀 `Agibuild.Modulus`**，并且按你的要求 **不做任何兼容映射**，一次性彻底切换到新标识符。

## What Changes

- **BREAKING**：全量引入前缀 `Agibuild.Modulus`，覆盖以下层级：
  - Solution 名称：`Agibuild.Modulus.sln`
  - Project 名称/路径/文件名：`Agibuild.Modulus.*`
  - AssemblyName：输出程序集 `Agibuild.Modulus.*.dll`
  - RootNamespace（含默认代码生成命名空间）：`Agibuild.Modulus.*`
  - Host Id（写入 `extension.vsixmanifest`）：  
    - `Agibuild.Modulus.Host.Avalonia`  
    - `Agibuild.Modulus.Host.Blazor`
- **BREAKING**：模块清单中所有与 Host 相关的标识符将被替换：
  - `InstallationTarget/@Id`
  - `Asset/@TargetHost`
  - 任何运行时/安装时校验中引用的 Host Id 常量/字符串
- **BREAKING**：不提供任何兼容处理（不接受旧 Host Id，不做旧→新映射，不提供迁移脚本）。
- 更新模板（VS / dotnet new / CLI 内置模板）使新生成项目默认使用新前缀与新 Host Id。
- 更新打包剔除共享程序集规则，使其匹配新的一方程序集名（避免因重命名导致共享策略失效）。

## Impact

- Affected specs:
  - `runtime`（Host Id、shared policy、安装/加载校验示例）
  - `module-template`（模板生成的引用与默认命名）
  - `module-packaging`（共享程序集剔除规则与示例）
- Affected code（实现阶段会涉及）：
  - `*.sln`
  - `src/**/*.csproj`、目录名、项目引用、`AssemblyName`/`RootNamespace`
  - `src/Hosts/*`（Host Id、启动注册、资源与配置）
  - `src/Modulus.Core/*`（manifest 校验、Host Id 常量/字符串引用）
  - `src/Modulus.Cli/Templates/*`、`templates/*`
  - `build/BuildTasks.cs`（如存在对旧名称的显式引用）


