# Troubleshooting Guide

This guide helps you resolve common issues you might encounter while working with Modulus.

## Installation Issues

### Missing .NET SDK
**Problem**: Error indicating .NET SDK is missing or version mismatch.
**Solution**: 
1. Install the latest .NET SDK (version 8.0 or higher) from [Microsoft's .NET download page](https://dotnet.microsoft.com/download).
2. Verify installation by running `dotnet --version` in your terminal.

### Template Installation Failure
**Problem**: `dotnet new modulus-app` or `dotnet new modulus-plugin` fails.
**Solution**:
1. Make sure you've installed the templates: `dotnet new install <path-to-templates-folder>`
2. Check for error messages in the installation output.
3. Try with administrator/elevated privileges.

## Build Issues

### Nuke Build Failures
**Problem**: `nuke build` command fails.
**Solution**:
1. Ensure Nuke.Global tool is installed: `dotnet tool install Nuke.GlobalTool --global`
2. Check build logs in the `artifacts/logs` directory for specific errors.
3. Verify all required SDKs and dependencies are installed.

### Missing Dependencies
**Problem**: Build fails with missing package references.
**Solution**:
1. Restore NuGet packages: `dotnet restore`
2. Check if your network connection can access NuGet repositories.
3. If using private packages, verify authentication is set up correctly.

## Plugin Issues

### Plugins Not Loading
**Problem**: Plugins don't appear in the application.
**Solution**:
1. Verify plugins are in the correct directory: `%USERPROFILE%/.modulus/plugins` or `~/.modulus/plugins`
2. Check plugin assembly implements `IPlugin` interface correctly.
3. Ensure plugin's contract version is compatible with the host.
4. Check application logs for plugin loading errors.

### Plugin Crashes
**Problem**: Plugin crashes during operation.
**Solution**:
1. Check logs for exceptions.
2. Verify all plugin dependencies are correctly resolved.
3. Make sure the plugin is compatible with the current host version.
4. Restart the application with the plugin disabled to isolate issues.

### Configuration Issues
**Problem**: Plugin settings are not being recognized.
**Solution**:
1. Verify `pluginsettings.json` is correctly formatted (valid JSON).
2. Check if the file is in the right location (same directory as the plugin dll).
3. Ensure settings keys match what the plugin is trying to access.

## Localization Issues

### Missing Translations
**Problem**: Text appears in default language instead of user's language.
**Solution**:
1. Verify `lang.xx.json` files exist for the desired language.
2. Check the language code matches the system or user-specified language.
3. Make sure translation keys in code match those in the language files.

### Encoding Problems
**Problem**: Special characters appear corrupted.
**Solution**:
1. Ensure all language files are saved with UTF-8 encoding.
2. Check for BOM (Byte Order Mark) issues in the files.

## UI Integration Issues

### Plugin UI Not Displaying
**Problem**: Plugin UI components don't appear in the main app.
**Solution**:
1. Check if `GetMainView()` or `GetMenu()` returns valid UI components.
2. Verify UI components follow the host's UI framework requirements.
3. Look for any layout or styling mismatches.

### Visual Glitches
**Problem**: Plugin UI doesn't match the application theme.
**Solution**:
1. Make sure plugin UI uses the host's theming system.
2. Avoid hardcoded colors or styles in the plugin UI.
3. Test with different theme settings.

## Debug and Advanced Troubleshooting

### Enabling Debug Logs
To get more detailed logs:
1. Set `MODULUS_LOG_LEVEL=Debug` environment variable.
2. Check logs in `%USERPROFILE%/.modulus/logs` or `~/.modulus/logs`.

### Debugging Plugins
To debug a plugin:
1. Set `MODULUS_DEBUG_PLUGIN=MyPlugin` environment variable.
2. Attach a debugger to the host process.
3. Set breakpoints in your plugin code.

### Reporting Issues
When reporting issues:
1. Include full error message and stack trace.
2. Provide reproduction steps.
3. Share plugin and host version information.
4. Attach relevant logs if possible.

## Need More Help?

If you're still experiencing issues:
- Check the [GitHub repository](https://github.com/Agibuild/modulus) for open issues.
- Join our community discussions.
- Contact the development team directly.
