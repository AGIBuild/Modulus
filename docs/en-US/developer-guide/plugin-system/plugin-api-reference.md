# Plugin API Reference

This document provides a detailed reference for the Modulus plugin API interfaces and their usage.

## Related Documentation

- [Plugin Development Guide](plugin-development-guide.md) - Main documentation for plugin development
- [Advanced Examples](plugin-advanced-examples.md) - Advanced code examples and patterns

## Core Interfaces

### IPlugin

The primary interface that all plugins must implement.

```csharp
public interface IPlugin
{
    /// <summary>
    /// Gets the plugin metadata.
    /// </summary>
    IPluginMeta Meta { get; }
    
    /// <summary>
    /// Configures services for the plugin.
    /// </summary>
    /// <param name="services">The service collection to register services with.</param>
    /// <param name="configuration">The configuration for the plugin.</param>
    void ConfigureServices(IServiceCollection services, IConfiguration configuration);
    
    /// <summary>
    /// Initializes the plugin with the resolved service provider.
    /// </summary>
    /// <param name="provider">The service provider containing registered services.</param>
    void Initialize(IServiceProvider provider);
    
    /// <summary>
    /// Gets the main view for the plugin. Return null if the plugin doesn't provide a UI.
    /// </summary>
    /// <returns>The main view object, typically an Avalonia control, or null.</returns>
    object? GetMainView();
    
    /// <summary>
    /// Gets menu items for the plugin. Return null if the plugin doesn't provide menu items.
    /// </summary>
    /// <returns>The menu items or null.</returns>
    object? GetMenu();
}
```

### IPluginMeta

Defines metadata for a plugin.

```csharp
public interface IPluginMeta
{
    /// <summary>
    /// Gets the display name of the plugin.
    /// </summary>
    string Name { get; }
    
    /// <summary>
    /// Gets the version of the plugin.
    /// </summary>
    string Version { get; }
    
    /// <summary>
    /// Gets a description of the plugin.
    /// </summary>
    string Description { get; }
    
    /// <summary>
    /// Gets the author of the plugin.
    /// </summary>
    string Author { get; }
    
    /// <summary>
    /// Gets an array of plugin IDs that this plugin depends on, or null if there are no dependencies.
    /// </summary>
    string[]? Dependencies { get; }
    
    /// <summary>
    /// Gets the plugin contract version required by this plugin.
    /// </summary>
    string ContractVersion { get; }
    
    /// <summary>
    /// Gets an optional icon for navigation, or null.
    /// </summary>
    string? NavigationIcon { get; }
    
    /// <summary>
    /// Gets an optional section for navigation grouping, or null.
    /// </summary>
    string? NavigationSection { get; }
    
    /// <summary>
    /// Gets the order of this plugin in navigation.
    /// </summary>
    int NavigationOrder { get; }
}
```

### ILocalizer

Provides localization services for plugins.

```csharp
public interface ILocalizer
{
    /// <summary>
    /// Gets the localized string for the specified key.
    /// </summary>
    /// <param name="key">The resource key.</param>
    /// <returns>The localized string, or the key itself if not found.</returns>
    string this[string key] { get; }
    
    /// <summary>
    /// Gets the current language code.
    /// </summary>
    string CurrentLanguage { get; }
    
    /// <summary>
    /// Sets the current language.
    /// </summary>
    /// <param name="language">The language code to set.</param>
    void SetLanguage(string language);
    
    /// <summary>
    /// Gets the collection of supported language codes.
    /// </summary>
    IEnumerable<string> SupportedLanguages { get; }
}
```

## Extension Points

### UI Integration

Plugins can integrate with the main application UI through:

1. **Main View**: Implement `GetMainView()` to return a UI control (typically an Avalonia control) that will be displayed in the main content area when the plugin is selected.

2. **Menu Extension**: Implement `GetMenu()` to return menu items that will be added to the application menu.

Example:

```csharp
public object? GetMainView()
{
    return new MyPluginView();
}

public object? GetMenu()
{
    return new List<MenuItemViewModel>
    {
        new MenuItemViewModel
        {
            Header = "My Plugin",
            Icon = "\uE8A5",
            Items = new List<MenuItemViewModel>
            {
                new MenuItemViewModel 
                { 
                    Header = "Action 1", 
                    Command = new RelayCommand(ExecuteAction1) 
                },
                new MenuItemViewModel 
                { 
                    Header = "Action 2", 
                    Command = new RelayCommand(ExecuteAction2) 
                }
            }
        }
    };
}
```

### Service Registration

Plugins can register their services during the `ConfigureServices` phase:

```csharp
public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
{
    // Register singleton services (one instance for the plugin's lifetime)
    services.AddSingleton<IMyService, MyService>();
    
    // Register transient services (new instance each time)
    services.AddTransient<IMyTransientService, MyTransientService>();
    
    // Use configuration
    services.Configure<MyOptions>(configuration.GetSection("MyOptions"));
}
```

## API Examples

### Basic Plugin Implementation

```csharp
public class MyPlugin : IPlugin
{
    private ILogger<MyPlugin>? _logger;
    
    public IPluginMeta Meta { get; } = new MyPluginMeta();
    
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IMyService, MyService>();
    }
    
    public void Initialize(IServiceProvider provider)
    {
        _logger = provider.GetRequiredService<ILogger<MyPlugin>>();
        _logger.LogInformation("Plugin {PluginName} initialized", Meta.Name);
    }
    
    public object? GetMainView() => new MyPluginView();
    
    public object? GetMenu() => null; // No menu extension
}

public class MyPluginMeta : IPluginMeta
{
    public string Name => "My Plugin";
    public string Version => "1.0.0";
    public string Description => "Example plugin for Modulus";
    public string Author => "Developer Name";
    public string[]? Dependencies => null;
    public string ContractVersion => "2.0.0";
    public string? NavigationIcon => "\uE8A5";
    public string? NavigationSection => "Tools";
    public int NavigationOrder => 100;
}
```

### Using Localization

```csharp
public class LocalizedService
{
    private readonly ILocalizer _localizer;
    
    public LocalizedService(ILocalizer localizer)
    {
        _localizer = localizer;
    }
    
    public string GetWelcomeMessage()
    {
        return _localizer["Welcome"];
    }
    
    public void SwitchToChineseLanguage()
    {
        _localizer.SetLanguage("zh");
    }
}
```

### Configuration Usage

```csharp
public class ConfigAwareService
{
    private readonly IConfiguration _configuration;
    
    public ConfigAwareService(IConfiguration configuration)
    {
        _configuration = configuration;
    }
    
    public void ProcessSettings()
    {
        var setting1 = _configuration["Settings:Setting1"];
        var setting2 = _configuration.GetValue<int>("Settings:Setting2");
        var connectionString = _configuration.GetConnectionString("DefaultConnection");
    }
}
```
