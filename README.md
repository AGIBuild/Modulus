# Modulus
Modulus - Modular Application Template for Cross-Platform Tooling

**Modulus** 是一个现代化的跨平台插件式工具应用模板，旨在帮助开发者快速构建可扩展、可维护、支持 AI 的桌面工具类软件。

该模板具备模块化架构、热插拔插件、配置系统、依赖注入、本地化、多版本兼容、签名验证等关键特性，帮助你专注业务开发而非基础设施。

---

## ✨ 特性亮点

- 🔌 插件热更新与动态卸载（基于 AssemblyLoadContext）
- ⚙️ 插件配置支持（JSON-based）
- 📦 插件依赖注入（DI 容器隔离）
- 🌐 多语言本地化（支持自动切换）
- 🔐 插件签名验证与版本控制
- 🧠 AI Agent 插件支持（可嵌入 LLM）
- 🛠️ 提供插件开发 SDK 与模板工程
- 🖥️ 跨平台支持：Windows / macOS（Avalonia UI）

---

## 📦 用途场景

- 构建桌面数据工具 / UI 自动化工具
- 快速构建开发者辅助类应用（Log Viewer、Code Generator）
- 面向 AI 插件开发的任务框架
- 内部工具平台（多团队协作）

---

## 🚀 快速开始

```bash
dotnet new --install Modulus.Templates
dotnet new modulus-plugin -n MyPlugin
```

---

## 📚 文档

完整的项目文档可以在 [docs](./docs/README.md) 目录中找到。文档包括：

- **用户指南**：安装说明、使用说明和故障排除
- **开发者指南**：插件开发、系统架构和 API 参考
- **用户故事**：产品开发路线图和功能演进

文档提供多种语言版本：
- [English Documentation](./docs/en-US/README.md)
- [中文文档](./docs/zh-CN/README.md)

---

## 📊 项目状态

查看当前项目进度与状态请参阅 [README-Project-Status.md](./README-Project-Status.md)。

我们使用标准化的用户故事文档，所有 Story 文件遵循以下命名规则：
- 文件格式：`S-XXXX-标题.md`
- 文档标题：`# S-XXXX-标题`
- 必要元数据：包含优先级和状态标记

通过运行 `.\Generate-StoryProgress.ps1` 可生成最新的项目进度报告。

---