# Project Context

## Purpose
Modulus is a modern, cross-platform, plugin-based application framework designed for building extensible, maintainable, and AI-ready tools. The core value proposition is "write once, run on multiple hosts" - allowing developers to implement UI-agnostic business logic that runs across different UI frameworks without modification.

## Tech Stack
- **Runtime**: .NET 10.0
- **UI Frameworks**:
  - Avalonia (Native desktop host)
  - Blazor Hybrid + MAUI (Web-style host)
- **Messaging**: MediatR (in-process communication)
- **Data**: Entity Framework Core + SQLite
- **Testing**: xUnit, NSubstitute
- **Build**: Nuke (C# build automation)

## Project Conventions

### Code Style
- C# with nullable enabled and implicit usings
- English only in code files (comments, strings, identifiers)
- Use `required` keyword for mandatory properties
- Use `init` accessors for immutable configuration objects
- JSON property names use camelCase (`[JsonPropertyName("...")]`)

### Architecture Patterns
- **Vertical Slice Architecture**: Each module contains:
  - `<Module>.Core` - Domain/Application layer (UI-agnostic)
  - `<Module>.UI.Avalonia` - Avalonia presentation layer
  - `<Module>.UI.Blazor` - Blazor presentation layer
- **Module Isolation**: AssemblyLoadContext-based isolation for hot reload/unload
- **Dependency Rule**: Core assemblies MUST NOT reference UI frameworks; use `Modulus.UI.Abstractions` for UI contracts
- **Cross-module Communication**: Always via MediatR, never direct dependencies

### Testing Strategy
- Unit tests in `tests/<Project>.Tests/` directories
- Use NSubstitute for mocking
- Test naming: `MethodName_Scenario_ExpectedResult`
- Integration tests for host-level functionality

### Git Workflow
- Feature branches: `feature/<change-id>` or `<story-id>-<name>`
- Commit messages: English only, concise and descriptive
- PR descriptions: Brief, clear structure, English only

## Domain Context
- **Module**: A vertical slice feature unit with Core + UI assemblies, identified by GUID
- **Host**: The shell application providing environment (window, navigation, menu) - currently Avalonia or Blazor
- **PluginPackage**: Deployable artifact containing manifest.json + assemblies (conceptually `.modpkg`)
- **Manifest**: JSON descriptor with id, version, supportedHosts, coreAssemblies, uiAssemblies

## Important Constraints
- Core/Application assemblies MUST be UI-agnostic (no direct Avalonia/Blazor references)
- System modules cannot be unloaded at runtime
- Module manifests must be versioned (`manifestVersion: "1.0"`)
- Host must only load UI assemblies matching its type
- **Blazor UI**: Prefer MudBlazor components over custom implementations; avoid reinventing the wheel
- **Avalonia UI**: Follow Avalonia component best practices (proper DataContext binding, TemplatedControl for reusable controls, StyledProperty for bindable properties)

## External Dependencies
- MediatR 13.1.0 - Inter-module messaging
- Microsoft.EntityFrameworkCore.Sqlite 10.0.0 - Local storage
- Microsoft.Extensions.DependencyInjection - DI container
- Microsoft.Extensions.Hosting - Host abstractions
- Avalonia 11.x - Desktop UI framework
- MudBlazor - Blazor UI component library
