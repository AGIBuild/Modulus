# Modulus 项目状态

本文档提供了 Modulus 项目的当前状态和进度概览。

## Story 命名规则

我们采用统一的 Story 命名规则：

- **文件格式**：`S-XXXX-标题.md` (例如：`S-0001-初始项目设置.md`)
- **标题格式**：`# S-XXXX-标题` (例如：`# S-0001-初始项目设置`)
- **必要元数据**：每个 Story 文档需包含优先级和状态标记

```markdown
<!-- 优先级：P0 | P1 | P2 -->
<!-- 状态：待开始 | 进行中 | 已完成 -->
```

## 如何生成最新进度报告

我们提供了自动化脚本来生成项目进度报告：

```powershell
# 默认生成报告（存放在 docs/reports 目录）
.\Update-ProjectDocs.ps1

# 指定报告输出目录
.\Update-ProjectDocs.ps1 -ReportPath ".\custom\path"

# 同时清理空文件
.\Update-ProjectDocs.ps1 -CleanEmptyFiles
```

这将生成以下报告文件：
- `docs/reports/story-progress-report.md` - 默认进度报告
- `docs/reports/story-progress-report.zh-CN.md` - 中文进度报告
- `docs/reports/story-progress-report.en-US.md` - 英文进度报告

## 项目管理文档

详细的项目管理指南可在以下位置找到：
- [项目管理指南](./docs/project-management.md)
- [Story 模板](./docs/story-template.md)

## 如何更新任务状态

1. 在相应的 Story 文档中，将已完成任务的复选框从 `[ ]` 更新为 `[x]`
2. 更新 Story 文档顶部的状态注释（<!-- 状态：待开始 | 进行中 | 已完成 -->）
3. 运行进度报告脚本查看最新状态

## GitHub 项目看板

您也可以在我们的 GitHub 仓库查看可视化的项目进度：
[https://github.com/Agibuild/modulus/projects](https://github.com/Agibuild/modulus/projects)
