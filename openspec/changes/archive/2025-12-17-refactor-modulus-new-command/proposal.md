# Change: Refactor `modulus new` command surface to dotnet-new style

## Why

当前 `modulus new` 将大量模板元数据（显示名称、发布者、图标、菜单顺序等）暴露为命令行参数，导致参数面过大、难以发现与难以脚本化，也与 `dotnet new` 的最佳实践不一致。

## What Changes

- 将 `modulus new` 重构为 **dotnet-new 风格**：
  - 以 **template** 驱动创建：`modulus new [<template>] -n <name> ...`
  - 提供 `--list` 列出可用模板
  - 未显式指定模板时，默认使用 `module-avalonia`
- **BREAKING**：移除不必要的参数（立即删除，不做兼容别名/隐藏参数）
  - 移除：`--target/-t`、`--display-name/-d`、`--description`、`--publisher/-p`、`--icon/-i`、`--order/-o`
- 统一创建命令的通用参数与语义
  - `-n|--name`：模块名（PascalCase）
  - `-o|--output`：输出目录（默认当前目录）
  - `-f|--force`：覆盖已有目录

## Impact

- Affected specs:
  - `openspec/specs/module-template/spec.md`（CLI 命令语法与行为）
  - `openspec/specs/cli-testing/spec.md`（new 命令测试用例语法）
- Affected code (expected):
  - `src/Modulus.Cli/Commands/NewCommand.cs`（参数定义、解析与行为）
  - `tests/Modulus.Cli.IntegrationTests/Infrastructure/CliRunner.cs`
  - `tests/Modulus.Cli.IntegrationTests/Commands/NewCommandTests.cs`
  - `docs/cli-reference.md`、`docs/getting-started*.md`、`README*.md`（命令示例）


