# Modulus Installation Guide

This guide provides detailed instructions for installing Modulus on various operating systems.

## System Requirements

- **Operating System**: Windows 10/11, macOS 10.15+, or Linux (Ubuntu 20.04+, Fedora 34+, or similar)
- **RAM**: Minimum 4GB, 8GB recommended
- **Disk Space**: At least 500MB for the application and basic plugins
- **Framework**: .NET 8.0 SDK or later
- **Development Environment** (optional): Visual Studio 2022 or VS Code with C# extension

## Installation Methods

There are several ways to install Modulus:

### Method 1: Using Pre-built Packages

1. Visit our [GitHub Releases page](https://github.com/Agibuild/modulus/releases)
2. Download the appropriate package for your operating system
3. Installation varies by platform:
   - **Windows**: Run the installer (.exe or .msi) and follow the on-screen instructions
   - **macOS**: Open the .dmg file and drag the application to your Applications folder
   - **Linux**: Extract the .tar.gz file or use the distribution-specific package (.deb, .rpm)

### Method 2: Building from Source

#### Prerequisites

- Git
- .NET 8.0 SDK or later

#### Steps

1. Clone the repository:
   ```
   git clone https://github.com/Agibuild/modulus.git
   ```

2. Navigate to the project directory:
   ```
   cd Modulus
   ```

3. Build the application:
   ```
   dotnet build
   ```

4. Run the application:
   ```
   dotnet run --project src/Modulus.App.Desktop/Modulus.App.Desktop.csproj
   ```

### Method 3: Using the Nuke Build System

For developers who want to use our custom build system:

1. Clone the repository:
   ```
   git clone https://github.com/Agibuild/modulus.git
   ```

2. Install the Nuke global tool (if not already installed):
   ```
   dotnet tool install Nuke.GlobalTool --global
   ```

3. Navigate to the project directory:
   ```
   cd Modulus
   ```

4. Build the application:
   ```
   nuke build
   ```

5. Run the application:
   ```
   nuke run
   ```

## Plugin Installation

Modulus supports plugins to extend functionality. Here's how to install them:

1. Open Modulus
2. Go to **Settings > Plugins > Browse Plugins**
3. Select the plugin you wish to install and click "Install"
4. Restart Modulus when prompted

### Manual Plugin Installation

You can also install plugins manually:

1. Download the plugin package (.zip or .mpkg)
2. Extract the contents to: 
   - Windows: `%USERPROFILE%\.modulus\plugins\[PluginName]`
   - macOS/Linux: `~/.modulus/plugins/[PluginName]`
3. Restart Modulus

## First-Time Setup

After installing Modulus, follow these steps for initial setup:

1. Launch the application
2. Complete the welcome wizard to set your preferences
3. Configure any required settings in the Settings menu
4. Install recommended plugins as needed

## Troubleshooting Installation Issues

If you encounter problems during installation, check these common solutions:

### Missing .NET SDK

**Problem**: Error indicates .NET SDK is missing
**Solution**: Install the .NET 8.0 SDK from [Microsoft's .NET download page](https://dotnet.microsoft.com/download)

### Permission Issues

**Problem**: Permission denied errors
**Solution**: Run the installer with administrator/sudo privileges

### Application Won't Start

**Problem**: Application fails to start after installation
**Solution**: Check the logs at:
- Windows: `%USERPROFILE%\.modulus\logs`
- macOS/Linux: `~/.modulus/logs`

For more troubleshooting help, see the [Troubleshooting Guide](./troubleshooting.md) or raise an issue on our GitHub repository.

## Updating Modulus

To update Modulus to the latest version:

1. For installed packages, use your system's update mechanism
2. For source builds, pull the latest code and rebuild:
   ```
   git pull
   dotnet build
   ```
3. For Nuke builds:
   ```
   git pull
   nuke build
   ```

## Uninstallation

To remove Modulus from your system:

- **Windows**: Use Add/Remove Programs in Control Panel
- **macOS**: Drag the application from Applications to Trash
- **Linux**: Use your package manager or remove the extracted directory

To completely remove all user data:
- Windows: Delete `%USERPROFILE%\.modulus`
- macOS/Linux: Delete `~/.modulus`
