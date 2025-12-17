## Context

本变更聚焦于 `modulus new` 的命令行体验，目标是收敛参数面并对齐 `dotnet new` 的使用模型：以 template 选择为主、以通用参数（name/output/force）为辅，避免将模板内部元数据暴露为 CLI 参数。

## Goals / Non-Goals

- Goals
  - 最小参数面：创建项目只需理解 template + name + output/force
  - 具备可发现性：支持 `modulus new --list` 列出模板
  - 默认行为明确：未指定 template 时默认 `module-avalonia`
  - 立即删除冗余参数（breaking change）
- Non-Goals
  - 不在本变更中引入更多模板（仅 `module-avalonia`/`module-blazor`）
  - 不在本变更中更改生成的文件内容语义（仅命令行入口与参数映射调整）

## Decisions

### Decision: New syntax

Primary syntax:

```bash
modulus new [<template>] -n <name> [-o <dir>] [-f] [--list]
```

- `<template>`: `module-avalonia` / `module-blazor`（可选；缺省时为 `module-avalonia`）
- `-n|--name`: 模块名（PascalCase）
- `-o|--output`: 输出目录（默认当前目录）
- `-f|--force`: 覆盖已有目录
- `--list`: 列出模板并退出（不创建项目）

### Decision: Immediate removal of legacy options

Deleted options (no compatibility layer):
- `--target/-t`
- `--display-name/-d`
- `--description`
- `--publisher/-p`
- `--icon/-i`
- `--order/-o`

Rationale: 这些参数属于模板内部元数据，应该由生成后修改文件/代码来完成，而不是创建命令的必选理解成本。

## Risks / Trade-offs

- **BREAKING**：脚本/文档中旧语法会失败，需要同步更新测试与文档。
- 模板元数据不可在创建时通过 CLI 注入，用户需改生成文件（但换来更稳定的命令面）。

## Migration Plan

- Update docs/examples and integration tests to:
  - `modulus new -n MyModule`（默认 `module-avalonia`）
  - `modulus new module-blazor -n MyModule`


