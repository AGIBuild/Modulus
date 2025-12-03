# Modulus

Modulus is a modern, cross-platform, plugin-based application framework designed to help developers quickly build extensible, maintainable, and AI-ready desktop tools.

## ✨ Features

### Dual-Host Architecture
- **Blazor Hybrid Host**: MAUI-based with MudBlazor UI components
- **Avalonia Host**: Native desktop experience with Avalonia UI
- **Shared Core Logic**: Same Domain/Application code runs on both hosts

### Plugin System
- Hot-reloadable and dynamically unloadable plugins (AssemblyLoadContext)
- Manifest-driven module discovery and loading
- Dependency injection for plugins (DI container isolation)
- System module protection (prevent accidental unloading)

### Developer Experience
- Plugin development SDK with declarative attributes
- AI Agent plugin support (LLM integration)
- Plugin signature verification and version control
- Cross-platform: Windows / macOS / Linux

## 🏗️ Architecture

```
src/
├── Modulus.Core/              # Runtime, module loader, DI
├── Modulus.Sdk/               # SDK base classes, attributes
├── Modulus.UI.Abstractions/   # UI contracts (IMenuRegistry, IThemeService)
├── Hosts/
│   ├── Modulus.Host.Blazor/   # Blazor Hybrid (MAUI + MudBlazor)
│   └── Modulus.Host.Avalonia/ # Avalonia desktop
└── Modules/
    ├── EchoPlugin/            # Example: Echo plugin
    └── SimpleNotes/           # Example: Notes module
```

## 📦 Use Cases
- Desktop data tools / UI automation tools
- Rapid development of developer utilities (Log Viewer, Code Generator)
- Task framework for AI plugin development
- Internal tool platforms (multi-team collaboration)

## 🚀 Getting Started

### Run Avalonia Host
```bash
dotnet run --project src/Hosts/Modulus.Host.Avalonia
```

### Run Blazor Host
```bash
dotnet run --project src/Hosts/Modulus.Host.Blazor
```

### Run Tests
```bash
dotnet test
```

## 🔌 Creating a Module

1. Create three projects: `MyModule.Core`, `MyModule.UI.Avalonia`, `MyModule.UI.Blazor`
2. Define your module class with `[Module]` attribute
3. Add UI-specific menu attributes (`[AvaloniaMenu]`, `[BlazorMenu]`)
4. Create `manifest.json` with module metadata

See [Quickstart Guide](./specs/001-core-architecture/quickstart.md) for detailed instructions.

## 🤖 AI-Assisted Development
Modulus includes a built-in system to bootstrap AI context for tools like GitHub Copilot:

```powershell
# Bootstrap AI context (for GitHub Copilot)
nuke StartAI

# Role-specific context
nuke StartAI --role Backend
nuke StartAI --role Frontend
nuke StartAI --role Plugin
```

## 📚 Documentation
- [Core Architecture Spec](./specs/001-core-architecture/spec.md)
- [Quickstart Guide](./specs/001-core-architecture/quickstart.md)
- [Data Model](./specs/001-core-architecture/data-model.md)
- [Runtime Contracts](./specs/001-core-architecture/contracts/runtime-contracts.md)

## Project Status
- Current Branch: `001-core-architecture`
- Phase: MVP Complete (User Stories 1-3)
- Test Coverage: 30 tests passing

## Contributing
Pull requests and issues are welcome! See [CONTRIBUTING.md](./CONTRIBUTING.md) for guidelines.
