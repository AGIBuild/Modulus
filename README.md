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