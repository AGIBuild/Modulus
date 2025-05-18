# Modulus Plugin Development Guide

This document provides detailed guidance for developing plugins on the Modulus platform, including plugin contracts, directory structure, foundational capabilities, and best practices.

## Table of Contents

1. [Plugin Contract](#plugin-contract)
2. [Directory Structure](#directory-structure)
3. [Core Interfaces](#core-interfaces)
4. [Plugin Lifecycle](#plugin-lifecycle)
5. [Dependency Injection & Service Registration](#dependency-injection--service-registration)
6. [Configuration System](#configuration-system)
7. [Localization Support](#localization-support)
8. [Logging](#logging)
9. [UI Integration](#ui-integration)
10. [Best Practices](#best-practices)

## Plugin Contract

The Modulus plugin system is based on clearly defined contract interfaces located in the `Modulus.Plugin.Abstractions` assembly. All plugins must implement these interfaces to be properly loaded and used by the main application.

- **IPlugin**: The main plugin interface, including metadata retrieval, service registration, initialization, and UI extension points
- **IPluginMeta**: Plugin metadata interface, containing name, version, description, author, and dependencies
- **ILocalizer**: Localization interface, supporting multi-language resource access and switching
- **IPluginSettings**: Plugin configuration interface, providing access to plugin-specific settings

## Directory Structure

Each plugin should have the following standardized directory structure:

```
MyPlugin/
  ├── MyPlugin.dll          # Main assembly
  ├── pluginsettings.json   # Plugin configuration
  ├── lang.en.json          # English language resources
  ├── lang.zh.json          # Chinese language resources
  └── [Other dependency DLLs]  # Other assemblies the plugin depends on
```

### Example Configuration File (pluginsettings.json)

```json
{
  "ContractVersion": "2.0.0",
  "Settings": {
    "MySetting1": "value1",
    "MySetting2": 123,
    "MySetting3": true
  }
}
```

### Example Language Resource File (lang.en.json)

```json
{
  "Hello": "Hello",
  "Goodbye": "Goodbye",
  "Welcome": "Welcome to Modulus",
  "Settings": "Settings"
}
```

## Plugin Lifecycle

1. **Discovery and Loading**: The main program scans the plugin directory, looking for assemblies implementing the `IPlugin` interface
2. **Version Compatibility Check**: Checks the plugin contract version compatibility with the main program
3. **Service Registration**: Calls the `ConfigureServices` method to register plugin services
4. **Initialization**: Calls the `Initialize` method for plugin initialization
5. **UI Integration**: Integrates plugin UI through the `GetMainView` and `GetMenu` methods
6. **Unloading**: Cleans up resources when the plugin is unloaded

## Dependency Injection & Service Registration

Plugins can register their own services through the `ConfigureServices` method:

```csharp
public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
{
    // Register plugin services
    services.AddSingleton<IMyService, MyService>();
    services.AddScoped<IMyDataAccess, MyDataAccess>();
    
    // Can use configuration
    var myOption = configuration.GetSection("Settings:MySetting1").Value;
    services.Configure<MyOptions>(configuration.GetSection("Settings"));
}
```

## Configuration System

Plugins can access configuration from `pluginsettings.json` through injected `IConfiguration`:

```csharp
public class MyService
{
    private readonly IConfiguration _configuration;
    
    public MyService(IConfiguration configuration)
    {
        _configuration = configuration;
    }
    
    public void DoSomething()
    {
        var setting1 = _configuration["Settings:MySetting1"];
        var setting2 = _configuration.GetValue<int>("Settings:MySetting2");
        // ...
    }
}
```

## Localization Support

Plugins can access localization resources through injected `ILocalizer`:

```csharp
public class MyView
{
    private readonly ILocalizer _localizer;
    
    public MyView(ILocalizer localizer)
    {
        _localizer = localizer;
        
        // Get localized string in current language
        var hello = _localizer["Hello"];
        
        // Get list of supported languages
        var languages = _localizer.SupportedLanguages;
        
        // Switch language
        _localizer.SetLanguage("zh");
    }
}
```

## Logging

Plugins can log through injected `ILogger<T>`:

```csharp
public class MyService
{
    private readonly ILogger<MyService> _logger;
    
    public MyService(ILogger<MyService> logger)
    {
        _logger = logger;
    }
    
    public void DoSomething()
    {
        _logger.LogInformation("Performing operation");
        
        try
        {
            // ...
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Operation failed");
        }
    }
}
```

## UI Integration

Plugins can integrate with the main application UI by implementing UI extension points:

```csharp
public class MyPlugin : IPlugin
{
    // ... other implementation ...
    
    public object? GetMainView()
    {
        // Return the main view for this plugin
        return new MyPluginView();
    }
    
    public object? GetMenu()
    {
        // Return menu extensions
        return new MyPluginMenu();
    }
}
```

## Best Practices

1. **Follow Dependency Injection Principles**: Use constructor injection for dependencies, avoid static access and service locator patterns
2. **Appropriate Isolation**: Layer your logic, separate UI, business logic, and data access
3. **Exception Handling**: Catch and log exceptions, don't let them propagate to the main program
4. **Resource Disposal**: Properly implement resource disposal patterns, release all resources when the plugin is unloaded
5. **Version Compatibility**: Correctly declare the plugin's contract version in `ContractVersion`
6. **Namespace Isolation**: Use unique namespace prefixes to avoid conflicts with other plugins or the main program
7. **UI Guidelines**: Follow the host application's UI style and patterns
8. **Performance**: Minimize startup impact by performing heavy initialization lazily

## Example Plugins

Reference sample plugin implementations:

- SimplePlugin: Basic plugin example
- ExamplePlugin: Complete functionality demonstration
- NavigationExamplePlugin: Navigation and UI extension example

## Troubleshooting Guide

- **Plugin Cannot Load**: Check contract version compatibility, directory structure, assembly references
- **Services Cannot Resolve**: Ensure services are registered correctly, check dependencies
- **Localization Not Working**: Check language file format and path
- **Configuration Invalid**: Check pluginsettings.json format and path
