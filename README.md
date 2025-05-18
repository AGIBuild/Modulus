# Modulus

Modulus is a modern, cross-platform, plugin-based application template designed to help developers quickly build extensible, maintainable, and AI-ready desktop tools.

## ✨ Features
- Hot-reloadable and dynamically unloadable plugins (AssemblyLoadContext)
- Plugin configuration support (JSON-based)
- Dependency injection for plugins (DI container isolation)
- Multi-language localization (automatic switching)
- Plugin signature verification and version control
- AI Agent plugin support (LLM integration)
- Plugin development SDK and project templates
- Cross-platform: Windows / macOS (Avalonia UI)

## 📦 Use Cases
- Desktop data tools / UI automation tools
- Rapid development of developer utilities (Log Viewer, Code Generator)
- Task framework for AI plugin development
- Internal tool platforms (multi-team collaboration)

## 🚀 Getting Started
```bash
dotnet new --install Modulus.Templates
dotnet new modulus-plugin -n MyPlugin
```

## 📚 Documentation
- [English Documentation](./docs/en-US/README.md)
- [中文文档 Chinese Documentation](./docs/zh-CN/README.md)

## Project Status
- See progress report: [docs/reports/story-progress-report.en-US.md](./docs/reports/story-progress-report.en-US.md)

## Story Naming Convention
- File format: `S-XXXX-Title.md`
- Document title: `# S-XXXX-Title`
- Required metadata: priority and status tags

## Contributing
Pull requests and issues are welcome!