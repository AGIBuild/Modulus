<!-- 优先级：P0 -->
<!-- 状态：已完成 -->
# S-0008-AI 上下文清单与 Nuke StartAI 一键注入

**用户故事**
作为一名使用 GitHub Copilot（或任何 AI agent）的开发者，我希望有一个统一的项目上下文清单和 Nuke 命令来一键注入 AI 上下文，这样 Copilot 能立即理解项目架构、规范和目标，无需人工反复说明，所有团队成员的 AI 协作体验也能保持一致。

**验收标准**
- 项目根目录存在 `ai-manifest.yaml`（或 `ai-manifest.json`），描述项目概览、架构、目录/命名规范、路线图和术语表。
- Nuke 提供 `StartAI` 目标，运行 `nuke StartAI` 可输出适合 Copilot/AI agent ingest 的最新上下文。
- 命令支持参数（如 `--role Frontend`）以按角色过滤上下文。
- pre-commit 和 CI 检查保证 manifest 变更与结构/规范同步。
- Onboarding/贡献指南明确要求新成员用 `nuke StartAI` 注入 AI 上下文。
- 团队成员可在 Copilot Chat 用 `/sync`、`/roadmap`、`/why <file>` 等指令快速获取上下文（引用 manifest 段落）。
- 每次新增 Story 必须同时提供中英文双版本文档，并作为 AI 上下文规范的一部分。

**技术任务**
- [x] 在根目录起草并维护 `ai-manifest.yaml`，内容包括：
    - 项目概览（愿景、特性、技术栈）
    - 架构（模块、DI、插件系统、数据流）
    - 目录与命名规范
    - 路线图/里程碑
    - 术语表与常见问题
- [x] 在 Nuke 构建脚本中添加 `StartAI` 目标。
- [x] 实现聚合 manifest、README、进度报告等上下文的输出逻辑。
- [x] 支持按角色过滤上下文（如 `nuke StartAI --role Backend`）。
- [x] 添加 pre-commit 和 CI 检查，保证 manifest 一致性。
- [x] 更新 Onboarding/贡献文档，说明 Copilot/AI 上下文注入方法。
- [x] （可选）实现 ManifestSync CLI，自动从代码生成/更新 manifest。
- [x] 明确 Story 文档编写规范：每次新增 Story 必须同时提供中英文双版本。

**说明**
- 该方案确保所有 Copilot/AI agent 用户都能获得一致、最新的项目上下文，最大化 AI 协作开发效率。
- 该规范适用于所有 Story 文档，要求每次新增 Story 必须有中英文双版本。
