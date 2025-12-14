## 1. Prerequisites

- [x] 1.1 修改 `CliServiceProvider.Build()` 支持 `databasePath` 参数
- [x] 1.2 修改 `CliServiceProvider.Build()` 支持 `modulesDirectory` 参数
- [x] 1.3 确保 `LocalStorage` 路径逻辑支持参数化

## 2. Test Infrastructure

- [x] 2.1 创建 `Modulus.Cli.IntegrationTests` 项目
- [x] 2.2 实现 `CliTestContext` 测试环境隔离类
  - [x] 临时目录管理
  - [x] 测试数据库初始化（运行 migrations）
  - [x] 资源清理（IDisposable）
- [x] 2.3 实现 `CliRunner` 命令执行器
  - [x] 进程执行模式（用于 new/build/pack）
  - [x] 直接调用模式（用于 install/uninstall/list）
- [x] 2.4 实现 `CliResult` 执行结果模型
- [x] 2.5 添加项目到解决方案

## 3. Command Tests

- [x] 3.1 实现 `NewCommandTests` (modulus new)
  - [x] 创建 Avalonia 模块成功
  - [x] 创建 Blazor 模块成功
  - [x] --force 覆盖已有目录
  - [x] 目录冲突时失败
- [x] 3.2 实现 `BuildCommandTests` (modulus build)
  - [x] 编译成功
  - [x] 编译失败检测
  - [x] Debug/Release 配置
- [x] 3.3 实现 `PackCommandTests` (modulus pack)
  - [x] 正常打包
  - [x] --no-build 选项
  - [x] -o 输出路径验证
  - [x] 包内容验证（解压检查）
- [x] 3.4 实现 `InstallCommandTests` (modulus install)
  - [x] 从 .modpkg 安装
  - [x] 从目录安装
  - [x] --force 覆盖安装
  - [x] 路径不存在失败
- [x] 3.5 实现 `UninstallCommandTests` (modulus uninstall)
  - [x] 按名称卸载
  - [x] 按 ID 卸载
  - [x] 不存在的模块失败
- [x] 3.6 实现 `ListCommandTests` (modulus list)
  - [x] 空列表
  - [x] 显示已安装模块
  - [x] --verbose 详细信息

## 4. End-to-End Tests

- [x] 4.1 实现 `FullLifecycleTests`
  - [x] Avalonia 模块：new → build → pack → install → list → uninstall → list(空)
  - [x] Blazor 模块：new → build → pack → install → list → uninstall → list(空)
- [x] 4.2 实现 `ModuleLoadTests`
  - [x] 使用 ModuleLoader 验证生成的 Avalonia 模块可加载
  - [x] 使用 ModuleLoader 验证生成的 Blazor 模块可加载

## 5. Build Integration

- [x] 5.1 添加 Nuke `TestCli` target
- [x] 5.2 更新 `PublishCli` target 依赖 `TestCli`（发布前必须测试通过）
- [x] 5.3 更新 `PackCli` target 依赖 `TestCli`（打包前必须测试通过）
