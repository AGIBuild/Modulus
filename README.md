# Modulus

Modulus is a modern, cross-platform, plugin-based application framework designed to help developers quickly build extensible, maintainable, and AI-ready tools.

## ✨ Features

### Multi-Host Architecture
- **UI-Agnostic Core**: Business logic independent of any UI framework
- **Pluggable Hosts**: Supports Avalonia (desktop) and Blazor Hybrid (MAUI)
- **Shared Core Logic**: Same Domain/Application code runs across all hosts

### Extension System
- **VS Extension Compatible**: Uses `extension.vsixmanifest` (XML) format
- **Hot-Reloadable**: AssemblyLoadContext-based isolation for dynamic load/unload
- **Explicit Installation**: Extensions installed via CLI or UI, not auto-discovered
- **Type-Safe Entry Points**: `ModulusPackage` base class similar to VS VsPackage

### Developer Experience
- Extension SDK with declarative attributes
- AI Agent plugin support (LLM integration)
- Signature verification and version control
- Cross-platform: Windows / macOS / Linux

## 🏗️ Architecture

```
src/
├── Modulus.Core/              # Runtime, module loader, DI
├── Modulus.Sdk/               # SDK: ModulusPackage, attributes
├── Modulus.UI.Abstractions/   # UI contracts (IMenuRegistry, INavigationService)
├── Hosts/
│   ├── Modulus.Host.Avalonia/ # Avalonia desktop (ID: Modulus.Host.Avalonia)
│   └── Modulus.Host.Blazor/   # Blazor Hybrid (ID: Modulus.Host.Blazor)
└── Modules/
    ├── EchoPlugin/            # Example: Echo plugin
    ├── SimpleNotes/           # Example: Notes module
    └── ComponentsDemo/        # Example: UI components demo
```

## 📦 Extension Structure

```
MyExtension/
├── extension.vsixmanifest     # XML manifest (VS Extension format)
├── MyExtension.Core.dll       # Core logic (host-agnostic)
├── MyExtension.UI.Avalonia.dll
└── MyExtension.UI.Blazor.dll
```

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

## 🔌 Creating an Extension

### 1. Create Projects

```
MyExtension/
├── MyExtension.Core/
├── MyExtension.UI.Avalonia/
└── MyExtension.UI.Blazor/
```

### 2. Define Entry Point

```csharp
// MyExtension.Core/MyExtensionPackage.cs
public class MyExtensionPackage : ModulusPackage
{
    public override void ConfigureServices(IModuleLifecycleContext context)
    {
        context.Services.AddSingleton<IMyService, MyService>();
    }
}
```

### 3. Create Manifest

```xml
<!-- extension.vsixmanifest -->
<?xml version="1.0" encoding="utf-8"?>
<PackageManifest Version="2.0.0" 
    xmlns="http://schemas.microsoft.com/developer/vsx-schema/2011">
  <Metadata>
    <Identity Id="your-guid" Version="1.0.0" Publisher="You" />
    <DisplayName>My Extension</DisplayName>
    <Description>My awesome extension</Description>
  </Metadata>
  <Installation>
    <InstallationTarget Id="Modulus.Host.Avalonia" Version="[1.0,)" />
    <InstallationTarget Id="Modulus.Host.Blazor" Version="[1.0,)" />
  </Installation>
  <Assets>
    <Asset Type="Modulus.Package" Path="MyExtension.Core.dll" />
    <Asset Type="Modulus.Package" Path="MyExtension.UI.Avalonia.dll" 
           TargetHost="Modulus.Host.Avalonia" />
    <Asset Type="Modulus.Menu" Id="my-menu" DisplayName="My Tool" 
           Icon="Home" Route="MyExtension.ViewModels.MainViewModel" 
           TargetHost="Modulus.Host.Avalonia" />
  </Assets>
</PackageManifest>
```

### 4. Install Extension

```bash
modulus install ./MyExtension
```

## 📚 Documentation

- [OpenSpec Specifications](./openspec/specs/)
- [Project Context](./openspec/project.md)
- [Contributing Guide](./CONTRIBUTING.md)

## Project Status

- **Phase**: Active Development
- **Test Coverage**: 30+ tests passing
- **Platforms**: Windows, macOS, Linux

## Contributing

Pull requests and issues are welcome! See [CONTRIBUTING.md](./CONTRIBUTING.md) for guidelines.

## License

[MIT License](./LICENSE)
