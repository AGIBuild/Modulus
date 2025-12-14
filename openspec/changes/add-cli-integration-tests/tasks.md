## 1. Prerequisites

- [ ] 1.1 修改 `CliServiceProvider.Build()` 支持 `databasePath` 参数
- [ ] 1.2 修改 `CliServiceProvider.Build()` 支持 `modulesDirectory` 参数
- [ ] 1.3 确保 `LocalStorage` 路径逻辑支持参数化

## 2. Test Infrastructure

- [ ] 2.1 创建 `Modulus.Cli.IntegrationTests` 项目
- [ ] 2.2 实现 `CliTestContext` 测试环境隔离类
  - [ ] 临时目录管理
  - [ ] 测试数据库初始化（运行 migrations）
  - [ ] 资源清理（IDisposable）
- [ ] 2.3 实现 `CliRunner` 命令执行器
  - [ ] 进程执行模式（用于 new/build/pack）
  - [ ] 直接调用模式（用于 install/uninstall/list）
- [ ] 2.4 实现 `CliResult` 执行结果模型
- [ ] 2.5 添加项目到解决方案

## 3. Command Tests

- [ ] 3.1 实现 `NewCommandTests` (modulus new)
  - [ ] 创建 Avalonia 模块成功
  - [ ] 创建 Blazor 模块成功
  - [ ] --force 覆盖已有目录
  - [ ] 目录冲突时失败
- [ ] 3.2 实现 `BuildCommandTests` (modulus build)
  - [ ] 编译成功
  - [ ] 编译失败检测
  - [ ] Debug/Release 配置
- [ ] 3.3 实现 `PackCommandTests` (modulus pack)
  - [ ] 正常打包
  - [ ] --no-build 选项
  - [ ] -o 输出路径验证
  - [ ] 包内容验证（解压检查）
- [ ] 3.4 实现 `InstallCommandTests` (modulus install)
  - [ ] 从 .modpkg 安装
  - [ ] 从目录安装
  - [ ] --force 覆盖安装
  - [ ] 路径不存在失败
- [ ] 3.5 实现 `UninstallCommandTests` (modulus uninstall)
  - [ ] 按名称卸载
  - [ ] 按 ID 卸载
  - [ ] 不存在的模块失败
- [ ] 3.6 实现 `ListCommandTests` (modulus list)
  - [ ] 空列表
  - [ ] 显示已安装模块
  - [ ] --verbose 详细信息

## 4. End-to-End Tests

- [ ] 4.1 实现 `FullLifecycleTests`
  - [ ] Avalonia 模块：new → build → pack → install → list → uninstall → list(空)
  - [ ] Blazor 模块：new → build → pack → install → list → uninstall → list(空)
- [ ] 4.2 实现 `ModuleLoadTests`
  - [ ] 使用 ModuleLoader 验证生成的 Avalonia 模块可加载
  - [ ] 使用 ModuleLoader 验证生成的 Blazor 模块可加载

## 5. Build Integration

- [ ] 5.1 添加 Nuke `TestCli` target
- [ ] 5.2 更新 `PublishCli` target 依赖 `TestCli`（发布前必须测试通过）
- [ ] 5.3 更新 `PackCli` target 依赖 `TestCli`（打包前必须测试通过）
