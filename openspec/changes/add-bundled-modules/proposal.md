# Change: 内置模块打包发布

## Why
框架 Owner 需要将某些模块作为 Host 的内置部分发布，使用户运行 Host 时即包含指定扩展。当前构建流程无法将模块打包进 Host 发布包。

## What Changes
- 新增 `bundled-modules.json` 配置文件声明内置模块
- 扩展 `nuke build` 流程，将指定模块复制到 Host 输出目录的 `Modules/` 子目录
- 内置模块标记为 `IsSystem: true`，用户无法卸载

## Impact
- Affected specs: 新增 `bundled-modules` capability
- Affected code:
  - `build/BuildTasks.cs` - 新增 BundleModules 目标
  - `src/Hosts/Modulus.Host.Avalonia/bundled-modules.json` - 新增配置文件
  - `src/Hosts/Modulus.Host.Blazor/bundled-modules.json` - 新增配置文件

