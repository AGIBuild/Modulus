## 1. Nuke 打包目标

- [x] 1.1 在 `build/BuildTasks.cs` 添加 `PackModule` 目标
- [x] 1.2 实现从 `extension.vsixmanifest` 读取版本号
- [x] 1.3 使用 `dotnet publish` 生成 self-contained 输出
- [x] 1.4 过滤共享程序集（Modulus.*, System.*, Microsoft.Extensions.*）
- [x] 1.5 创建 ZIP 压缩包到 `artifacts/packages/`
- [x] 1.6 支持 `--name` 参数打包单个模块
- [x] 1.7 包含 README.md 和 LICENSE.txt（如存在）

## 2. CLI 项目结构

- [x] 2.1 创建 `src/Modulus.Cli/Modulus.Cli.csproj` 项目
- [x] 2.2 添加依赖：`System.CommandLine`, `Modulus.Core`, `Modulus.Infrastructure.Data`
- [x] 2.3 配置输出到 `artifacts/` 目录
- [x] 2.4 实现 `Program.cs` 入口点和命令注册
- [x] 2.5 实现 `CliServiceProvider` 配置 DI 容器（数据库、日志、安装服务）

## 3. CLI install 命令

- [x] 3.1 实现 `InstallCommand.cs`
- [x] 3.2 支持从 `.modpkg` 文件安装（解压 ZIP）
- [x] 3.3 支持从目录安装（复制文件）
- [x] 3.4 读取并验证 `extension.vsixmanifest`
- [x] 3.5 检测目标目录已存在时提示覆盖
- [x] 3.6 实现 `--force` 参数跳过确认
- [x] 3.7 复制文件到 `%APPDATA%/Modulus/Modules/{ModuleId}/`
- [x] 3.8 运行 EF migrations 确保数据库表存在
- [x] 3.9 调用 `ModuleInstallerService.InstallFromPathAsync` 写入数据库
- [x] 3.10 输出安装结果信息

## 4. CLI uninstall 命令

- [x] 4.1 实现 `UninstallCommand.cs`
- [x] 4.2 支持按模块名称或 ID (GUID) 卸载
- [x] 4.3 从数据库查询模块信息
- [x] 4.4 删除前提示确认
- [x] 4.5 实现 `--force` 参数跳过确认
- [x] 4.6 删除模块目录
- [x] 4.7 从数据库删除 `ModuleEntity` 和关联的 `MenuEntity`

## 5. CLI list 命令

- [x] 5.1 实现 `ListCommand.cs`
- [x] 5.2 从数据库查询所有已注册模块
- [x] 5.3 格式化输出模块信息（名称、版本、ID、状态）
- [x] 5.4 支持 `--verbose` 参数显示详细信息（路径、安装时间等）

## 6. Host 版本兼容性验证

- [x] 6.1 在 `RuntimeContext` 添加 `HostVersion` 属性
- [x] 6.2 更新 Avalonia Host 启动代码注册 HostVersion
- [x] 6.3 更新 Blazor Host 启动代码注册 HostVersion
- [x] 6.4 扩展 `DefaultManifestValidator` 验证 InstallationTarget 版本范围
- [x] 6.5 CLI install 输出 Host 版本兼容性警告

## 7. 集成与文档

- [x] 7.1 更新 `Modulus.sln` 添加 CLI 项目
- [x] 7.2 更新 `nuke build` 目标包含 CLI 构建
- [x] 7.3 添加 CLI 使用示例到 CONTRIBUTING.md (skipped - no doc changes per user rules)
