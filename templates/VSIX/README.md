# Modulus Templates VSIX Extension

This directory contains the Visual Studio Extension (VSIX) project for Modulus module templates.

## Prerequisites

- Windows OS
- Visual Studio 2022 with "Visual Studio extension development" workload
- .NET Framework 4.7.2 SDK

## Building the VSIX

### Option 1: Using Nuke (Recommended)

```powershell
# From the repository root
nuke pack-vsix
```

### Option 2: Manual Build

1. First, create the project template ZIP files:

```powershell
# Navigate to templates directory
cd templates

# Create Avalonia template ZIP
Compress-Archive -Path "VisualStudio\ModulusModule.Avalonia\*" -DestinationPath "VSIX\ProjectTemplates\ModulusModule.Avalonia.zip" -Force

# Create Blazor template ZIP
Compress-Archive -Path "VisualStudio\ModulusModule.Blazor\*" -DestinationPath "VSIX\ProjectTemplates\ModulusModule.Blazor.zip" -Force
```

2. Open `Modulus.Templates.Vsix.csproj` in Visual Studio 2022

3. Build in Release mode

4. The `.vsix` file will be in `bin\Release\`

## Publishing to VS Marketplace

1. Go to [Visual Studio Marketplace Publisher Portal](https://marketplace.visualstudio.com/manage/publishers)
2. Create or select your publisher
3. Click "New Extension" → "Visual Studio"
4. Upload the `.vsix` file
5. Fill in the metadata and submit

## Installing Locally

Double-click the `.vsix` file or run:

```powershell
VSIXInstaller.exe Modulus.Templates.vsix
```

## Using the Templates

After installation, in Visual Studio:
1. File → New → Project
2. Search for "Modulus"
3. Select one of:
   - "Modulus Module (Avalonia)"
   - "Modulus Module (Blazor)"
   - "Modulus Host App (Avalonia)"
   - "Modulus Host App (Blazor Hybrid)"
4. Enter project name and create

Notes:
- The Host App templates are equivalent to:
  - `modulus new avaloniaapp -n MyApp`
  - `modulus new blazorapp -n MyApp`
- "Modulus Host App (Blazor Hybrid)" is a **MAUI** template and typically requires **Windows** to build.

