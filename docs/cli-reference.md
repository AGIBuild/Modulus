# Modulus CLI Reference

The Modulus CLI (`modulus`) is a command-line tool for creating, building, packaging, and managing Modulus modules.

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
| `modulus new` | Create a new module project |
| `modulus build` | Build the module project |
| `modulus pack` | Package the module for distribution |
| `modulus install` | Install a module |
| `modulus uninstall` | Uninstall a module |
| `modulus list` | List installed modules |

---

## modulus new

Create a new Modulus module project with all necessary files and structure.

### Syntax

```bash
modulus new <name> [options]
```

### Arguments

| Argument | Required | Description |
|----------|----------|-------------|
| `<name>` | Yes | The module name (used for project and namespace) |

### Options

| Option | Short | Description | Default |
|--------|-------|-------------|---------|
| `--target` | `-t` | Target host: `avalonia` or `blazor` | (interactive) |
| `--display-name` | `-d` | Display name shown in menus | Same as name |
| `--description` | | Module description | Generated |
| `--publisher` | `-p` | Publisher name | (interactive) |
| `--icon` | `-i` | Menu icon (IconKind value) | `Folder` |
| `--order` | `-o` | Menu order | `100` |
| `--output` | | Output directory | Current directory |
| `--force` | `-f` | Overwrite existing directory | `false` |

### Available Icons

Common icon values: `Folder`, `Home`, `Settings`, `Terminal`, `Code`, `Database`, `Cloud`, `User`, `Star`, `Heart`, `Search`, `Edit`, `Delete`, `Add`, `Check`, `Close`, `Info`, `Warning`, `Error`

See the full list in `Modulus.UI.Abstractions.IconKind` enum.

### Examples

```bash
# Interactive mode (prompts for all options)
modulus new MyModule

# Create Avalonia module with all options
modulus new MyModule -t avalonia -d "My Module" -p "Acme Corp" -i Home

# Create Blazor module in specific directory
modulus new MyModule -t blazor --output ./src/Modules/

# Overwrite existing project
modulus new MyModule -t avalonia --force
```

### Generated Structure

```
MyModule/
├── MyModule.sln
├── .gitignore
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
- **macOS**: `~/Library/Application Support/Modulus/Modules/{ModuleId}/`
- **Linux**: `~/.local/share/Modulus/Modules/{ModuleId}/`

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
#     Path: /Users/you/.local/share/Modulus/Modules/MyModule
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
| `MODULUS_HOME` | Override default Modulus data directory |

## See Also

- [Getting Started](./getting-started.md)
- [Module Development Guide](./module-development.md)
- [Manifest Format](./manifest-format.md)

