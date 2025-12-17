# cli-testing Specification (Delta)

## MODIFIED Requirements

### Requirement: CLI Command Tests

CLI 集成测试 SHALL 覆盖所有 CLI 命令的基本功能。

#### Scenario: New command creates module
- **WHEN** 执行 `modulus new -n TestModule --force`
- **THEN** 创建包含正确结构的模块目录
- **AND** 生成 `.sln`、`.csproj`、`extension.vsixmanifest` 等文件

#### Scenario: Build command compiles module
- **WHEN** 在模块目录执行 `modulus build`
- **THEN** 编译成功并生成 DLL 文件
- **AND** 返回退出码 0

#### Scenario: Pack command creates package
- **WHEN** 在模块目录执行 `modulus pack`
- **THEN** 生成 `.modpkg` 文件
- **AND** 包内包含 `extension.vsixmanifest` 和模块 DLL

#### Scenario: Install command registers module
- **WHEN** 执行 `modulus install <path>.modpkg`
- **THEN** 解压包到模块目录
- **AND** 在数据库中注册模块
- **AND** `modulus list` 显示已安装模块

#### Scenario: Uninstall command removes module
- **WHEN** 执行 `modulus uninstall <name> --force`
- **THEN** 从数据库中删除模块记录
- **AND** `modulus list` 不再显示该模块

#### Scenario: List command shows installed modules
- **WHEN** 已安装模块后执行 `modulus list`
- **THEN** 显示模块名称和版本
- **WHEN** 执行 `modulus list --verbose`
- **THEN** 额外显示模块 ID、发布者等详细信息


