# Change: 模块打包与安装功能

## Why
当前 Modulus 框架缺少将模块项目打包为可分发格式的能力。开发者需要手动复制文件来部署模块，缺乏标准化的打包流程和 CLI 安装支持。

## What Changes
- 新增 `nuke pack-module` 构建目标，将模块打包为 `.modpkg` 文件
- 打包格式为 ZIP 压缩包，包含清单、程序集和所有依赖
- 文件名包含版本号：`{ModuleName}-{Version}.modpkg`
- 新增 `modulus install` CLI 命令支持从 `.modpkg` 文件安装模块
- 新增 `modulus uninstall` CLI 命令卸载已安装模块
- 增强 `InstallationTarget` 版本范围验证，确保模块与 Host 版本兼容

## Impact
- Affected specs: 新增 `module-packaging` capability，修改 `runtime` capability
- Affected code:
  - `build/BuildTasks.cs` - 新增 PackModule 目标
  - 新增 `src/Modulus.Cli/` 项目 - CLI 工具
  - `src/Modulus.Core/Installation/` - 安装服务扩展
  - `src/Modulus.Core/Runtime/RuntimeContext.cs` - 新增 HostVersion 属性
  - `src/Modulus.Core/Manifest/DefaultManifestValidator.cs` - 增强版本验证

