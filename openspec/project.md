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

## Domain Context

### Core Concepts

| Term | Description |
|------|-------------|
| **Extension** | A deployable unit containing manifest + assemblies (`.modpkg` package) |
| **Host** | Shell application providing environment - `Modulus.Host.Avalonia` or `Modulus.Host.Blazor` |
| **Package** | Entry point class inheriting `ModulusPackage`, similar to VS VsPackage |
| **Manifest** | `extension.vsixmanifest` (XML) describing extension metadata |

### Extension Structure

```
MyExtension/
├── extension.vsixmanifest      # XML manifest (vsixmanifest 2.0 format)
├── MyExtension.Core.dll        # Core logic (host-agnostic)
├── MyExtension.UI.Avalonia.dll # Avalonia UI (optional)
└── MyExtension.UI.Blazor.dll   # Blazor UI (optional)
```

### Manifest Format

Extensions use VS Extension compatible `extension.vsixmanifest` (XML):

```xml
<PackageManifest Version="2.0.0" xmlns="http://schemas.microsoft.com/developer/vsx-schema/2011">
  <Metadata>
    <Identity Id="guid" Version="1.0.0" Publisher="..." />
    <DisplayName>My Extension</DisplayName>
  </Metadata>
  <Installation>
    <InstallationTarget Id="Modulus.Host.Avalonia" Version="[1.0,)" />
  </Installation>
  <Assets>
    <Asset Type="Modulus.Package" Path="MyExtension.Core.dll" />
    <Asset Type="Modulus.Menu" Id="..." DisplayName="..." Route="..." />
  </Assets>
</PackageManifest>
```

### Asset Types

| Type | Purpose |
|------|---------|
| `Modulus.Package` | Assembly containing `ModulusPackage` entry points |
| `Modulus.Assembly` | Dependency assembly (no entry point scan) |
| `Modulus.Menu` | Menu declaration (parsed at install, not runtime) |
| `Modulus.Icon` | Extension icon |

### Host IDs

| Host | ID |
|------|-----|
| Avalonia | `Modulus.Host.Avalonia` |
| Blazor | `Modulus.Host.Blazor` |

## Architecture

### Vertical Slice

Each extension follows vertical slice architecture:
- `<Extension>.Core` - Domain/Application layer (UI-agnostic)
- `<Extension>.UI.Avalonia` - Avalonia presentation layer
- `<Extension>.UI.Blazor` - Blazor presentation layer

### Module Isolation

- Every extension runs in its own `ModuleLoadContext` (custom `AssemblyLoadContext`)
- Shared assemblies load from host's default context: `System.*`, `Microsoft.*`, `Avalonia*`, `Modulus.Core`, `Modulus.Sdk`, `Modulus.UI.*`

### Extension Lifecycle

```
Install → Database → Load → Initialize → Active
                              ↓
                         Shutdown → Unload
```

1. **Install**: Parse manifest, validate, write to database (no assembly loading)
2. **Load**: Create ALC, load assemblies, discover `ModulusPackage` entry points
3. **Initialize**: Execute lifecycle methods (`ConfigureServices` → `OnApplicationInitializationAsync`)

## Project Conventions

### Code Style

- C# with nullable enabled and implicit usings
- English only in code files (comments, strings, identifiers)
- Use `required` keyword for mandatory properties
- Use `init` accessors for immutable configuration objects

### Entry Point Classes

```csharp
// Recommended: inherit ModulusPackage
public class MyExtensionPackage : ModulusPackage
{
    public override void ConfigureServices(IModuleLifecycleContext context)
    {
        context.Services.AddSingleton<IMyService, MyService>();
    }
}

// Legacy: ModulusComponent still works but is [Obsolete]
public class MyExtensionModule : ModulusComponent { }  // ⚠️ Warning
```

### Testing

- Unit tests in `tests/<Project>.Tests/`
- Use NSubstitute for mocking
- Test naming: `MethodName_Scenario_ExpectedResult`

### Git Workflow

- Feature branches: `feature/<change-id>`
- Commit messages: English only, concise and descriptive

### OpenSpec 文档规范

- 所有 OpenSpec 文档使用**中文**编写
- Requirement/Scenario 标题使用英文
- 代码引用、文件路径、技术术语保持英文

## Important Constraints

- Core assemblies MUST NOT reference UI frameworks
- System extensions cannot be unloaded at runtime
- Extensions require explicit installation (no directory scanning)
- Host loads only UI assemblies matching its type (`TargetHost` attribute)
- Menu declarations in manifest, not assembly attributes

## External Dependencies

| Package | Version | Purpose |
|---------|---------|---------|
| MediatR | 13.1.0 | Inter-module messaging |
| Microsoft.EntityFrameworkCore.Sqlite | 10.0.0 | Local storage |
| Microsoft.Extensions.DependencyInjection | - | DI container |
| Avalonia | 11.x | Desktop UI framework |
| MudBlazor | - | Blazor UI components |
