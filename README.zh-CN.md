# Modulus 项目简介

Modulus 是一个现代化的跨平台插件式工具应用模板，旨在帮助开发者快速构建可扩展、可维护、支持 AI 的桌面工具类软件。

## ✨ 特性亮点
- 插件热更新与动态卸载（基于 AssemblyLoadContext）
- 插件配置支持（JSON-based）
- 插件依赖注入（DI 容器隔离）
- 多语言本地化（支持自动切换）
- 插件签名验证与版本控制
- AI Agent 插件支持（可嵌入 LLM）
- 提供插件开发 SDK 与模板工程
- 跨平台支持：Windows / macOS（Avalonia UI）

## 📦 用途场景
- 构建桌面数据工具 / UI 自动化工具
- 快速构建开发者辅助类应用（Log Viewer、Code Generator）
- 面向 AI 插件开发的任务框架
- 内部工具平台（多团队协作）

## 🚀 快速开始
```bash
dotnet new --install Modulus.Templates
dotnet new modulus-plugin -n MyPlugin
```

## 🤖 AI 辅助开发
Modulus 内置了项目上下文引导系统，用于支持 GitHub Copilot 等 AI 工具：

```powershell
# 引导 AI 上下文（用于 GitHub Copilot）
nuke StartAI

# 特定角色的上下文
nuke StartAI --role Backend
nuke StartAI --role Frontend
nuke StartAI --role Plugin
```

更多信息，请参阅 [CONTRIBUTING.zh-CN.md](./CONTRIBUTING.zh-CN.md)。

## 📚 文档
- [English Documentation](./docs/en-US/README.md)

## 项目状态
- 进度报告见 [docs/reports/story-progress-report.zh-CN.md](./docs/reports/story-progress-report.zh-CN.md)

## Story 命名规则
- 文件格式：`S-XXXX-标题.md`
- 文档标题：`# S-XXXX-标题`
- 必要元数据：包含优先级和状态标记

## 贡献
欢迎提交 issue 和 PR！
