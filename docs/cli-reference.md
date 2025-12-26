# Modulus CLI Reference

The Modulus CLI (`modulus`) is a command-line tool for creating, building, packaging, and managing Modulus projects (modules and host apps).

## Installation

```bash
# Install from NuGet
dotnet tool install -g Agibuild.Modulus.Cli

# Update to latest version
dotnet tool update -g Agibuild.Modulus.Cli

# Uninstall
dotnet tool uninstall -g Agibuild.Modulus.Cli
```

## Commands Overview

| Command | Description |
|---------|-------------|
| `modulus new` | Create a new project (module or host app) |
| `modulus build` | Build the module project |
| `modulus pack` | Package the module for distribution |
| `modulus install` | Install a module |
| `modulus uninstall` | Uninstall a module |
| `modulus list` | List installed modules |

---

## modulus new

Create a new Modulus project with all necessary files and structure.

### Syntax

```bash
modulus new [<template>] -n <name> [options]
```

### Arguments

| Argument | Required | Description |
|----------|----------|-------------|
| `<template>` | No | Template name: `avaloniaapp`, `blazorapp`, `module-avalonia`, `module-blazor` (default: `module-avalonia`) |

### Options

| Option | Short | Description | Default |
|--------|-------|-------------|---------|
| `--name` | `-n` | The module name (used for project and namespace) | (required) |
| `--output` | `-o` | Output directory | Current directory |
| `--force` | `-f` | Overwrite existing directory | `false` |
| `--list` | | List available templates and exit | `false` |

### Templates

- `avaloniaapp`: Modulus host app (Avalonia)
- `blazorapp`: Modulus host app (Blazor Hybrid / MAUI)
- `module-avalonia`: Modulus module (Avalonia)
- `module-blazor`: Modulus module (Blazor)

### Examples

```bash
# List templates
modulus new --list

# Create an Avalonia host app
modulus new avaloniaapp -n MyApp

# Create a Blazor Hybrid (MAUI) host app
modulus new blazorapp -n MyApp

# Create an Avalonia module (default template)
modulus new -n MyModule

# Create a Blazor module in a specific directory
modulus new module-blazor -n MyModule -o ./src/Modules/

# Overwrite existing project
modulus new -n MyModule --force
```

### Generated Structure

#### Module project (module-avalonia / module-blazor)

```
MyModule/
├── MyModule.sln
├── .gitignore
├── Directory.Build.props
├── extension.vsixmanifest
├── MyModule.Core/
│   ├── MyModule.Core.csproj
│   ├── MyModuleModule.cs
│   └── ViewModels/
│       └── MainViewModel.cs
└── MyModule.UI.Avalonia/  (or UI.Blazor)
    ├── MyModule.UI.Avalonia.csproj
    ├── MyModuleAvaloniaModule.cs
    ├── MainView.axaml
    └── MainView.axaml.cs
```

#### Host app project (avaloniaapp / blazorapp)

```
MyApp/
├── MyApp.sln
├── .gitignore
├── Directory.Build.props
└── MyApp.Host.Avalonia/ (or MyApp.Host.Blazor)
    ├── appsettings.json
    └── ... host entrypoint + UI files ...
```

**Notes:**
- `blazorapp` is a **MAUI** host template and typically requires **Windows** to build.

#### Host app details

**Avalonia host app (`avaloniaapp`)**

- **Project**: `MyApp.Host.Avalonia/`
- **Typical entrypoint**: `Program.cs`
- **Typical UI**: `App.axaml`, `MainWindow.axaml`
- **Build & run**:

```bash
cd MyApp
dotnet build -c Release
dotnet run --project MyApp.Host.Avalonia -c Release
```

**Blazor Hybrid (MAUI) host app (`blazorapp`)**

- **Project**: `MyApp.Host.Blazor/`
- **Typical entrypoint**: `MauiProgram.cs` + platform entry under `Platforms/`
- **Build prerequisites**:
  - Windows machine
  - .NET MAUI workload installed (for example: `dotnet workload install maui`)
- **Build & run**:

```bash
cd MyApp
dotnet build -c Release
dotnet run --project MyApp.Host.Blazor -c Release
```

See also:
- [`docs/host-app-development.md`](./host-app-development.md)

---

## modulus build

Build the module project in the current directory or specified path.

### Syntax

```bash
modulus build [options]
```

### Options

| Option | Short | Description | Default |
|--------|-------|-------------|---------|
| `--path` | `-p` | Path to module project | Current directory |
| `--configuration` | `-c` | Build configuration | `Release` |
| `--verbose` | `-v` | Show detailed output | `false` |

### Examples

```bash
# Build in current directory
modulus build

# Build Debug configuration
modulus build -c Debug

# Build specific project with verbose output
modulus build -p ./MyModule --verbose
```

---

## modulus pack

Build and package the module into a `.modpkg` file for distribution.

### Syntax

```bash
modulus pack [options]
```

### Options

| Option | Short | Description | Default |
|--------|-------|-------------|---------|
| `--path` | `-p` | Path to module project | Current directory |
| `--output` | `-o` | Output directory for .modpkg | `./output` |
| `--configuration` | `-c` | Build configuration | `Release` |
| `--no-build` | | Skip build step | `false` |
| `--verbose` | `-v` | Show detailed output | `false` |

### Package Contents

The generated `.modpkg` file is a ZIP archive containing:

- `extension.vsixmanifest` - Module manifest
- `*.dll` - Module assemblies (Core + UI)
- Third-party dependency DLLs
- `README.md`, `LICENSE.txt` (if present)

**Excluded from package:**
- Modulus shared assemblies (Core, SDK, UI.*)
- .NET framework assemblies
- Build artifacts (`.pdb`, etc.)

### Examples

```bash
# Build and pack
modulus pack

# Pack to specific directory
modulus pack -o ./dist

# Pack without rebuilding
modulus pack --no-build

# Verbose output
modulus pack --verbose
```

### Output

```
✓ Packaging complete!
  Output: ./output/MyModule-1.0.0.modpkg

To install this module:
  modulus install "./output/MyModule-1.0.0.modpkg"
```

---

## modulus install

Install a module from a `.modpkg` file or directory.

### Syntax

```bash
modulus install <source> [options]
```

### Arguments

| Argument | Required | Description |
|----------|----------|-------------|
| `<source>` | Yes | Path to `.modpkg` file or module directory |

### Options

| Option | Short | Description | Default |
|--------|-------|-------------|---------|
| `--force` | `-f` | Overwrite existing installation | `false` |
| `--verbose` | `-v` | Show detailed output | `false` |

### Installation Location

Modules are installed to:
- **Windows**: `%APPDATA%\Modulus\Modules\{ModuleId}\`
- **macOS/Linux**: `~/.modulus/Modules/{ModuleId}/`

### Examples

```bash
# Install from .modpkg file
modulus install ./MyModule-1.0.0.modpkg

# Install from directory (development)
modulus install ./artifacts/bin/Modules/MyModule/

# Force overwrite
modulus install ./MyModule-1.0.0.modpkg --force
```

---

## modulus uninstall

Uninstall a module by name or ID.

### Syntax

```bash
modulus uninstall <module> [options]
```

### Arguments

| Argument | Required | Description |
|----------|----------|-------------|
| `<module>` | Yes | Module name or ID (GUID) |

### Options

| Option | Short | Description | Default |
|--------|-------|-------------|---------|
| `--force` | `-f` | Skip confirmation prompt | `false` |
| `--verbose` | `-v` | Show detailed output | `false` |

### Examples

```bash
# Uninstall by name
modulus uninstall MyModule

# Uninstall by ID
modulus uninstall a1b2c3d4-e5f6-4a7b-8c9d-0e1f2a3b4c5d

# Skip confirmation
modulus uninstall MyModule --force
```

---

## modulus list

List all installed modules.

### Syntax

```bash
modulus list [options]
```

### Options

| Option | Short | Description | Default |
|--------|-------|-------------|---------|
| `--verbose` | `-v` | Show detailed information | `false` |

### Examples

```bash
# Basic list
modulus list

# Output:
# Installed Modules:
#   MyModule v1.0.0 - My Module Description

# Detailed list
modulus list --verbose

# Output:
# Installed Modules:
#   MyModule v1.0.0
#     ID: a1b2c3d4-e5f6-4a7b-8c9d-0e1f2a3b4c5d
#     Path: ~/.modulus/Modules/MyModule
#     Installed: 2025-12-14 10:30:00
```

---

## Exit Codes

| Code | Description |
|------|-------------|
| `0` | Success |
| `1` | General error |
| `2` | Invalid arguments |

## Environment Variables

| Variable | Description |
|----------|-------------|
| `MODULUS_CLI_DATABASE_PATH` | Override CLI database path (primarily for tests/automation) |
| `MODULUS_CLI_MODULES_DIR` | Override CLI modules directory (primarily for tests/automation) |

## See Also

- [Getting Started](./getting-started.md)
- [Module Development Guide](./module-development.md)

