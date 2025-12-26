# Host App Development Guide

This guide explains how to create and customize a **Modulus Host App** (a plugin-based application) using either the CLI or Visual Studio templates.

## What is a Host App?

- A **Host App** is the executable application that loads and runs Modulus modules.
- A **Module** is a separately built and packaged plugin (`.modpkg`) that the host installs and loads.

## Create a Host App

### Option 1: CLI (recommended)

```bash
# Avalonia desktop host app
modulus new avaloniaapp -n MyApp

# Blazor Hybrid (MAUI) host app
modulus new blazorapp -n MyApp
```

### Option 2: Visual Studio templates (wizard)

Use the Visual Studio project templates that ship with Modulus templates.
See: [`templates/VSIX/README.md`](../templates/VSIX/README.md)

## Generated Structure

Both `avaloniaapp` and `blazorapp` generate a solution folder like:

```
MyApp/
├── MyApp.sln
├── .gitignore
├── Directory.Build.props
├── appsettings.json
└── MyApp.Host.Avalonia/        # or: MyApp.Host.Blazor
    └── ... host project files ...
```

## Build & Run

### Avalonia host (`avaloniaapp`)

```bash
cd MyApp
dotnet build -c Release
dotnet run --project MyApp.Host.Avalonia -c Release
```

### Blazor Hybrid host (`blazorapp`)

`blazorapp` is a **.NET MAUI** host template and typically requires **Windows** to build.

Prerequisites:

```bash
dotnet workload install maui
```

Build & run:

```bash
cd MyApp
dotnet build -c Release
dotnet run --project MyApp.Host.Blazor -c Release
```

## Configuration (`appsettings.json`)

The generated host app reads configuration from `appsettings.json` in the app base directory.

### Database name

```json
{
  "Modulus": {
    "DatabaseName": "MyApp"
  }
}
```

The template computes a default database path from `DatabaseName`.
If you need a custom location, set `ModulusHostSdkOptions.DatabasePath` in code.

### Shared assembly policy (runtime + packaging)

Host apps define the **canonical shared assembly policy** via:

- `Modulus:Runtime:SharedAssemblies` (exact simple assembly names)
- `Modulus:Runtime:SharedAssemblyPrefixes` (prefix rules for framework families)

Example:

```json
{
  "Modulus": {
    "Runtime": {
      "SharedAssemblies": [
        "Modulus.Core",
        "Modulus.Sdk",
        "Modulus.UI.Abstractions",
        "Modulus.UI.Avalonia",
        "Modulus.HostSdk.Abstractions",
        "Modulus.HostSdk.Runtime"
      ],
      "SharedAssemblyPrefixes": [
        "Avalonia",
        "AvaloniaUI.",
        "SkiaSharp",
        "HarfBuzzSharp"
      ]
    }
  }
}
```

Guidance:

- Prefer **exact names** for Modulus/Host SDK assemblies.
- Use **prefixes** only for stable framework families (e.g., Avalonia, MAUI, MudBlazor).
- Avoid overly broad prefixes (they can accidentally “share” assemblies that should be isolated per module).

## Module Directories (where the host loads modules from)

The generated templates use `ModulusHostSdkBuilder.AddDefaultModuleDirectories()` which adds:

- **System modules**: `{AppBaseDir}/Modules`
- **User modules (Windows)**: `%APPDATA%/Modulus/Modules`
- **User modules (macOS/Linux)**: `~/.modulus/Modules`

### CLI alignment note

The Modulus CLI installs modules under:

- Windows: `%APPDATA%/Modulus/Modules`
- macOS/Linux: `~/.modulus/Modules`

The default Host App templates are aligned with this location.
If you are building a custom host and need to add it explicitly:

```csharp
using Modulus.Core.Paths;

// ...
sdkBuilder.AddModuleDirectory(
    path: Path.Combine(LocalStorage.GetUserRoot(), "Modules"),
    isSystem: false);
```

## Host Identity & Version (module compatibility)

Modules declare compatibility using `<InstallationTarget Id="..." Version="[min,max)" />` in `extension.vsixmanifest`.

The generated templates use stable host IDs:

- `Modulus.Host.Avalonia`
- `Modulus.Host.Blazor`

and use the host app assembly version as the host version.

If you create your own host:

- Keep the **Host ID stable** (treat it as a public contract).
- Use **SemVer** for host versioning.

## Next steps

- Module development: [`docs/module-development.md`](./module-development.md)
- CLI reference: [`docs/cli-reference.md`](./cli-reference.md)


