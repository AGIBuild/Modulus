# Contributing to Modulus

Thank you for your interest in contributing to Modulus! This guide will help you get started with contributing to the project.

## Architecture Overview

Modulus is a modular .NET application framework with a **multi-host architecture**:

### Core Principles

1. **UI-Agnostic Core**: Domain and Application code must not depend on any UI framework
2. **Multi-Host Support**: Same business logic runs across all supported hosts (Blazor, Avalonia, and future hosts)
3. **Vertical Slice Modules**: Each feature is a self-contained module with its own layers
4. **Dependency Pyramid**: Presentation → UI Abstraction → Application → Domain → Infrastructure

### Project Structure

```
src/
├── Modulus.Core/              # Runtime, ModuleLoader, DI, MediatR
├── Modulus.Sdk/               # SDK base classes (ModuleBase, attributes)
├── Modulus.UI.Abstractions/   # UI contracts (IMenuRegistry, IThemeService)
├── Hosts/
│   ├── Modulus.Host.Blazor/   # MAUI + MudBlazor
│   └── Modulus.Host.Avalonia/ # Avalonia UI
└── Modules/
    └── <ModuleName>/
        ├── <ModuleName>.Core/         # Domain + Application (UI-agnostic)
        ├── <ModuleName>.UI.Avalonia/  # Avalonia views
        └── <ModuleName>.UI.Blazor/    # Blazor components
```

### Module Development

When creating a new module:

1. **Core Project**: Contains ViewModels and business logic, references only `Modulus.Sdk` and `Modulus.UI.Abstractions`
2. **UI Projects**: Host-specific views, reference Core project and UI framework
3. **Manifest**: `manifest.json` describes module metadata and assembly mappings
4. **Attributes**: Use `[Module]`, `[AvaloniaMenu]`, `[BlazorMenu]` for declarative registration

See [Quickstart Guide](./specs/001-core-architecture/quickstart.md) for detailed instructions.

## Getting Started

1. Fork the repository
2. Clone your fork: `git clone https://github.com/your-username/modulus.git`
3. Create a new branch: `git checkout -b feature/your-feature-name`
4. Make your changes
5. Run tests: `dotnet test`
6. Commit your changes: `git commit -m "Add feature"`
7. Push to your fork: `git push origin feature/your-feature-name`
8. Create a pull request

## Documentation Standards

- All user-facing documentation should be in both English and Chinese
- All story documents must have bilingual versions (in `docs/en-US/stories/` and `docs/zh-CN/stories/`)
- Follow the story naming convention: `S-XXXX-Title.md`
- Include priority and status tags in story documents

## Code Style Guidelines

- Use PascalCase for class names and public members
- Use camelCase for local variables and parameters  
- Prefix private fields with underscore (`_privateField`)
- Include XML documentation for public APIs
- Write unit tests for all new functionality

## Building and Running

### Quick Start
```bash
# Run Avalonia Host
dotnet run --project src/Hosts/Modulus.Host.Avalonia

# Run Blazor Host  
dotnet run --project src/Hosts/Modulus.Host.Blazor

# Run all tests
dotnet test
```

### Nuke Build System
- Use the Nuke build system: `nuke --help` for available targets
- Build all components: `nuke build`
- Run tests: `nuke test`
- Pack plugins: `nuke plugin`

## Pull Request Process

1. Ensure your code follows the project's style guidelines
2. Update documentation as needed
3. Include tests for new functionality  
4. Make sure all tests pass before submitting
5. Link any related issues in your PR description
6. Wait for review from project maintainers

## Need Help?

If you have any questions, feel free to open an issue or join our community channels.
