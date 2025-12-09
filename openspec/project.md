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

### Module Load Context
- Every module runs inside its own `ModuleLoadContext` (custom `AssemblyLoadContext`) so it can be unloaded independently.
- A shared-assembly allowlist forces critical assemblies to load from the host’s default context. This list currently includes `System.*`, `Microsoft.*`, `Avalonia*`, `Modulus.Core`, `Modulus.Sdk`, `Modulus.UI.Abstractions`, and `Modulus.UI.Avalonia`.
- UI libraries that expose controls, styles, or resources (e.g., `Modulus.UI.Avalonia`) MUST live on the shared list; otherwise modules load a second copy of the assembly, causing `ControlTemplate` lookups and `AssetLoader` queries to fail because the `TargetType` and resource indices come from different runtime types.
- Symptoms of a missing shared assembly include `TemplatedParent` being null, `AssetLoader.Exists(avares://...)` returning false, and styles not applying even though resources are embedded.
- When adding a new shared UI/core library, update `ModuleLoadContext.IsSharedAssembly` and redeploy the host so modules reuse the same assembly instance.

### Testing Strategy
- Unit tests in `tests/<Project>.Tests/` directories
- Use NSubstitute for mocking
- Test naming: `MethodName_Scenario_ExpectedResult`
- Integration tests for host-level functionality

### OpenSpec 文档规范
- 所有 OpenSpec 文档（proposal.md, tasks.md, design.md, spec.md）使用**中文**编写
- Requirement/Scenario 标题使用英文
- Scenario 内容使用中文描述
- 代码引用、文件路径、技术术语保持英文

### Git Workflow
- Feature branches: `feature/<change-id>` or `<story-id>-<name>`
- Commit messages: English only, concise and descriptive
- PR descriptions: Brief, clear structure, English only

## Domain Context
- **Extension**: A vertical slice feature unit with Core + UI assemblies, identified by GUID (deployable as `.modpkg`)
- **Host**: The shell application providing environment (window, navigation, menu) - `Modulus.Host.Avalonia` or `Modulus.Host.Blazor`
- **Package**: Entry point class inheriting `ModulusPackage`, similar to VS VsPackage
- **Manifest**: `extension.vsixmanifest` (XML) with Identity, InstallationTarget, Dependencies, Assets

## Important Constraints
- Core/Application assemblies MUST be UI-agnostic (no direct Avalonia/Blazor references)
- System extensions cannot be unloaded at runtime
- Extensions require explicit installation (no directory scanning)
- Host loads only UI assemblies matching its type (`TargetHost` attribute in manifest)
- Menu declarations in manifest Assets, not assembly attributes
- **Blazor UI**: Prefer MudBlazor components over custom implementations; avoid reinventing the wheel
- **Avalonia UI**: Follow Avalonia component best practices (proper DataContext binding, TemplatedControl for reusable controls, StyledProperty for bindable properties)

## External Dependencies
- MediatR 13.1.0 - Inter-module messaging
- Microsoft.EntityFrameworkCore.Sqlite 10.0.0 - Local storage
- Microsoft.Extensions.DependencyInjection - DI container
- Microsoft.Extensions.Hosting - Host abstractions
- Avalonia 11.x - Desktop UI framework
- MudBlazor - Blazor UI component library
