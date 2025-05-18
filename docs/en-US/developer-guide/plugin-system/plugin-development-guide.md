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
10. [Development & Packaging](#development--packaging)
11. [Plugin Security & Isolation](#plugin-security--isolation)
12. [Advanced Features](#advanced-features)
13. [Best Practices](#best-practices)
14. [Troubleshooting Guide](#troubleshooting-guide)
15. [Performance Optimization](#performance-optimization)
16. [Example Plugins](#example-plugins)
17. [Conclusion](#conclusion)
18. [Step-by-Step Tutorials](#step-by-step-tutorials)

## Plugin Contract

The Modulus plugin system is based on clearly defined contract interfaces located in the `Modulus.Plugin.Abstractions` assembly. All plugins must implement these interfaces to be properly loaded and used by the main application.

### Plugin Architecture Diagram

The following diagram illustrates the architecture of the Modulus plugin system and how plugins interact with the main application:

```
┌───────────────────────────────────────────────────────────────────┐
│                       Modulus Host Application                     │
├───────────────┬───────────────────────────┬─────────────────────┬─┘
│ Plugin Loader │ Plugin Management Console │ Core Services       │
└─────┬─────────┴───────────────────────────┴─────────────────────┘
      │
      │                ┌───────────────┐     ┌───────────────┐
      └────Load────────► AssemblyLoad  │     │ AssemblyLoad  │
                       │ Context 1     │     │ Context 2     │
                       ├───────────────┤     ├───────────────┤
                       │ Plugin 1      │     │ Plugin 2      │
        Integration    ├───────────────┤     ├───────────────┤
      ◄───Interface────┤ IPlugin       │     │ IPlugin       │
                       ├───────────────┤     ├───────────────┤
                       │ Plugin UI     │     │ Plugin UI     │
                       ├───────────────┤     ├───────────────┤
                       │ Plugin        │     │ Plugin        │
                       │ Services      │     │ Services      │
                       └─────┬─────────┘     └─────┬─────────┘
                             │                     │
                             │                     │
                       ┌─────▼─────────────────────▼─────┐
                       │      Plugin Resources           │
                       │  (Config, Files, Local Data)    │
                       └───────────────────────────────┬─┘
```

This architecture provides:
- **Isolation**: Each plugin runs in its own `AssemblyLoadContext`, preventing conflicts
- **Standardized Integration**: Plugins use well-defined interfaces to interact with the host
- **Service Access**: Plugins can consume host services and provide plugin-specific services
- **UI Integration**: Plugins can contribute UI elements to the application

### Core Interfaces

- **IPlugin**: The main plugin interface, including metadata retrieval, service registration, initialization, and UI extension points
- **IPluginMeta**: Plugin metadata interface, containing name, version, description, author, and dependencies
- **ILocalizer**: Localization interface, supporting multi-language resource access and switching
- **IPluginSettings**: Plugin configuration interface, providing access to plugin-specific settings

### Interface Details

#### IPlugin Interface

```csharp
public interface IPlugin
{
    /// <summary>
    /// Gets the plugin metadata (name, version, author, etc).
    /// </summary>
    IPluginMeta GetMetadata();

    /// <summary>
    /// Register plugin services into the DI container.
    /// </summary>
    void ConfigureServices(IServiceCollection services, IConfiguration configuration);
    
    /// <summary>
    /// Called after DI container is built. Use to resolve services and perform initialization.
    /// </summary>
    void Initialize(IServiceProvider provider);
    
    /// <summary>
    /// Returns the main view/control for UI plugins (optional).
    /// </summary>
    object? GetMainView();
    
    /// <summary>
    /// Returns a menu or menu extension for the host (optional).
    /// </summary>
    object? GetMenu();
}
```

#### IPluginMeta Interface

```csharp
public interface IPluginMeta
{
    /// <summary>
    /// Plugin name.
    /// </summary>
    string Name { get; }
    
    /// <summary>
    /// Plugin version.
    /// </summary>
    string Version { get; }
    
    /// <summary>
    /// Plugin description.
    /// </summary>
    string Description { get; }
    
    /// <summary>
    /// Plugin author.
    /// </summary>
    string Author { get; }
    
    /// <summary>
    /// List of plugin dependencies (optional).
    /// </summary>
    string[]? Dependencies { get; }
    
    /// <summary>
    /// The contract version this plugin was built for.
    /// </summary>
    string ContractVersion { get; }
    
    /// <summary>
    /// Icon character for navigation menu (optional).
    /// This should be a character from an icon font like Segoe MDL2 Assets.
    /// </summary>
    string? NavigationIcon { get; }
    
    /// <summary>
    /// Section where the plugin should appear in the navigation bar (optional).
    /// Can be "header", "body", or "footer". Defaults to "body" if not specified.
    /// </summary>
    string? NavigationSection { get; }
    
    /// <summary>
    /// Order/position of the plugin in the navigation section (optional).
    /// Lower numbers appear first. Default is 100.
    /// </summary>
    int NavigationOrder { get; }
}
```

#### ILocalizer Interface

```csharp
public interface ILocalizer
{
    /// <summary>
    /// Gets a localized string by key.
    /// </summary>
    string this[string key] { get; }
    
    /// <summary>
    /// The current language code (e.g. "en", "zh").
    /// </summary>
    string CurrentLanguage { get; }
    
    /// <summary>
    /// Switch the current language.
    /// </summary>
    void SetLanguage(string lang);
    
    /// <summary>
    /// List of supported language codes.
    /// </summary>
    IEnumerable<string> SupportedLanguages { get; }
}
```

#### IPluginSettings Interface

```csharp
public interface IPluginSettings
{
    /// <summary>
    /// The configuration for the plugin.
    /// </summary>
    IConfiguration Configuration { get; }
}

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

### Detailed Process

1. **Discovery Phase**:
   - Modulus scans the user plugin directory (typically `%USERPROFILE%\.modulus\plugins\` on Windows or `~/.modulus/plugins/` on macOS/Linux)
   - The system discovers potential plugins by detecting DLL files in the directory
   - It also looks for associated `pluginsettings.json` configuration files

2. **Loading and Validation Phase**:
   - Plugins are loaded in a dedicated `AssemblyLoadContext` ensuring code isolation
   - The system looks for non-abstract classes implementing the `IPlugin` interface as plugin entry points
   - Plugin contract version compatibility is verified against the main application

3. **Service Registration and Initialization Phase**:
   - The system calls the plugin's `ConfigureServices` method, allowing the plugin to register its services into the DI container
   - A plugin-specific service provider is built
   - The plugin's `Initialize` method is called, allowing it to perform initialization tasks

4. **UI Integration Phase**:
   - The main application calls the plugin's `GetMainView` method to retrieve the plugin's main UI
   - It calls the plugin's `GetMenu` method to get plugin menu items (if any)
   - The plugin is integrated into the main UI based on navigation metadata (icon, position, etc.)

5. **Hot Reload Monitoring**:
   - The system monitors the plugin directory for changes (file creation, modification, deletion)
   - When changes are detected, plugin reload processes are triggered
   
6. **Unloading Phase**:
   - When a plugin needs to be unloaded (e.g., application shutdown or plugin update), the system attempts to release plugin resources
   - The plugin's `AssemblyLoadContext` is unloaded, reclaiming memory

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

### Service Lifetimes

When registering services, you can choose different lifetimes:

- **Singleton**: Creates a single instance shared throughout the application lifetime
  ```csharp
  services.AddSingleton<IMyService, MyService>();
  ```

- **Scoped**: Creates one instance per scope (typically one instance per request)
  ```csharp
  services.AddScoped<IMyDataAccess, MyDataAccess>();
  ```

- **Transient**: Creates a new instance each time requested
  ```csharp
  services.AddTransient<IMyProcessor, MyProcessor>();
  ```

### Accessing Host Services

Plugins can access services from the host application through the `Initialize` method:

```csharp
public void Initialize(IServiceProvider provider)
{
    // Get a logger
    var logger = provider.GetService<ILogger<MyPluginEntry>>();
    
    // Get the plugin's localizer
    var localizer = provider.GetService<ILocalizer>();
    
    // Get the host application's main window (if available)
    var mainWindow = provider.GetService<IMainWindow>();
}
```

### Service Registration Best Practices

1. **Use Extension Methods**: Place service registration in extension methods for cleaner code

   ```csharp
   // ServiceCollectionExtensions.cs
   public static class ServiceCollectionExtensions
   {
       public static IServiceCollection AddMyPluginServices(this IServiceCollection services)
       {
           services.AddSingleton<IMyService, MyService>();
           services.AddScoped<IMyDataAccess, MyDataAccess>();
           return services;
       }
   }
   
   // PluginEntry.cs
   public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
   {
       services.AddMyPluginServices();
   }
   ```

2. **Avoid Service Conflicts**: Use unique names to avoid conflicts with the main application or other plugins

3. **Use Delegate Registration**: For services that need configuration, use delegate registration

   ```csharp
   services.AddSingleton<IMyConfigurableService>(sp => 
   {
       var config = sp.GetRequiredService<IConfiguration>();
       var logger = sp.GetRequiredService<ILogger<MyService>>();
       return new MyConfigurableService(config["Settings:Key"], logger);
   });
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

Plugins can integrate with the main application UI through two main approaches: main views and menu extensions. Modulus uses Avalonia UI as its UI framework, allowing plugins to create rich cross-platform user interfaces.

### Main View Integration

By implementing the `GetMainView()` method, plugins can return a view that will be displayed in the main interface:

```csharp
public object? GetMainView()
{
    // Create and return a new view instance
    return new Views.MyPluginView();
}
```

#### Basic Avalonia View Example

A view is typically an Avalonia control. Here's a simple example of a view in XAML:

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             xmlns:vm="using:MyPlugin.ViewModels"
             mc:Ignorable="d" d:DesignWidth="800" d:DesignHeight="450"
             x:Class="MyPlugin.Views.MyPluginView">
    
    <Design.DataContext>
        <!-- This is used for design-time preview in editors -->
        <vm:MyPluginViewModel />
    </Design.DataContext>
    
    <Grid RowDefinitions="Auto,*">
        <StackPanel Grid.Row="0" Margin="20">
            <TextBlock Text="{Binding WelcomeMessage}" 
                      HorizontalAlignment="Center" 
                      VerticalAlignment="Center" 
                      FontSize="24" 
                      FontWeight="Bold"
                      Margin="0,0,0,10" />
            <TextBlock Text="{Binding DescriptionMessage}"
                      HorizontalAlignment="Center"
                      TextWrapping="Wrap"
                      Margin="0,0,0,20" />
        </StackPanel>
        
        <StackPanel Grid.Row="1" Margin="20" Spacing="10" HorizontalAlignment="Center">
            <Button Content="{Binding ButtonText}"
                   Command="{Binding ButtonCommand}"
                   HorizontalAlignment="Center"
                   HorizontalContentAlignment="Center"
                   Width="200" />
            
            <TextBlock Text="{Binding StatusMessage}"
                      HorizontalAlignment="Center"
                      Margin="0,10,0,0" />
        </StackPanel>
    </Grid>
</UserControl>
```

#### MVVM Pattern Implementation

For better separation of concerns, use the MVVM pattern with a proper ViewModel:

```csharp
// ViewModel/MyPluginViewModel.cs
using System.Windows.Input;
using ReactiveUI;
using Modulus.Plugin.Abstractions;

namespace MyPlugin.ViewModels
{
    public class MyPluginViewModel : ViewModelBase
    {
        private readonly ILocalizer _localizer;
        private readonly IMyPluginService _service;
        private string _statusMessage = string.Empty;
        
        public MyPluginViewModel(ILocalizer localizer, IMyPluginService service)
        {
            _localizer = localizer;
            _service = service;
            
            ButtonCommand = ReactiveCommand.Create(ExecuteButtonCommand);
        }
        
        public string WelcomeMessage => _localizer["Welcome"];
        public string DescriptionMessage => _localizer["PluginDescription"];
        public string ButtonText => _localizer["ExecuteAction"];
        
        public string StatusMessage 
        { 
            get => _statusMessage;
            private set => this.RaiseAndSetIfChanged(ref _statusMessage, value);
        }
        
        public ICommand ButtonCommand { get; }
        
        private void ExecuteButtonCommand()
        {
            var result = _service.PerformAction();
            StatusMessage = $"{_localizer["ActionResult"]}: {result}";
        }
    }
}
```

Corresponding C# code for the view:

```csharp
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Modulus.Plugin.Abstractions;
using MyPlugin.ViewModels;

namespace MyPlugin.Views
{
    public partial class MyPluginView : UserControl
    {
        public MyPluginView()
        {
            InitializeComponent();
            
            // Get services from DI container
            var serviceProvider = Program.ServiceProvider;
            var localizer = serviceProvider.GetRequiredService<ILocalizer>();
            var service = serviceProvider.GetRequiredService<IMyPluginService>();
            
            // Set data context to view model
            DataContext = new MyPluginViewModel(localizer, service);
        }
        
        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
```

### Advanced UI Controls

Modulus plugins can utilize the full range of Avalonia UI controls, including:

#### Data Grid Example

```xml
<DataGrid Items="{Binding Items}" 
          AutoGenerateColumns="False"
          GridLinesVisibility="All"
          BorderThickness="1" 
          BorderBrush="Gray"
          Margin="20">
    <DataGrid.Columns>
        <DataGridTextColumn Header="{Binding Source={StaticResource Localizer}, Path=[Id]}" 
                            Binding="{Binding Id}" 
                            Width="100" />
        <DataGridTextColumn Header="{Binding Source={StaticResource Localizer}, Path=[Name]}" 
                            Binding="{Binding Name}" 
                            Width="*" />
        <DataGridTextColumn Header="{Binding Source={StaticResource Localizer}, Path=[Value]}" 
                            Binding="{Binding Value}" 
                            Width="150" />
        <DataGridTemplateColumn Header="{Binding Source={StaticResource Localizer}, Path=[Actions]}"
                                Width="120">
            <DataGridTemplateColumn.CellTemplate>
                <DataTemplate>
                    <Button Content="{Binding Source={StaticResource Localizer}, Path=[Edit]}"
                            Command="{Binding DataContext.EditCommand, RelativeSource={RelativeSource AncestorType=DataGrid}}"
                            CommandParameter="{Binding}"
                            HorizontalAlignment="Center" />
                </DataTemplate>
            </DataGridTemplateColumn.CellTemplate>
        </DataGridTemplateColumn>
    </DataGrid.Columns>
</DataGrid>
```

#### Tabbed Interface Example

```xml
<TabControl>
    <TabItem Header="{Binding Source={StaticResource Localizer}, Path=[Overview]}">
        <StackPanel Margin="20">
            <TextBlock Text="{Binding OverviewText}" TextWrapping="Wrap" />
        </StackPanel>
    </TabItem>
    <TabItem Header="{Binding Source={StaticResource Localizer}, Path=[Details]}">
        <ScrollViewer>
            <StackPanel Margin="20">
                <TextBlock Text="{Binding DetailText}" TextWrapping="Wrap" />
            </StackPanel>
        </ScrollViewer>
    </TabItem>
    <TabItem Header="{Binding Source={StaticResource Localizer}, Path=[Settings]}">
        <StackPanel Margin="20" Spacing="10">
            <CheckBox Content="{Binding Source={StaticResource Localizer}, Path=[EnableFeature]}" 
                      IsChecked="{Binding IsFeatureEnabled}" />
            <TextBox Watermark="{Binding Source={StaticResource Localizer}, Path=[EnterValue]}" 
                     Text="{Binding ConfigValue}" />
            <Button Content="{Binding Source={StaticResource Localizer}, Path=[SaveSettings]}" 
                    Command="{Binding SaveSettingsCommand}" 
                    HorizontalAlignment="Right" />
        </StackPanel>
    </TabItem>
</TabControl>
```

### Menu Extensions

By implementing the `GetMenu()` method, plugins can add their own menu items to the main application's menu:

```csharp
public object? GetMenu()
{
    // Return menu extensions
    return new[] 
    {
        new MenuItemViewModel
        {
            Header = _localizer["PluginMenu"],
            Icon = "\uE8A5",  // Document icon from Segoe MDL2 Assets
            Items = new[]
            {
                new MenuItemViewModel
                {
                    Header = _localizer["NewItem"],
                    Icon = "\uE710", // Add icon
                    Command = new RelayCommand(CreateNewItem)
                },
                new MenuItemViewModel
                {
                    Header = _localizer["Settings"],
                    Icon = "\uE713", // Settings icon
                    Command = new RelayCommand(ShowSettings)
                },
                new MenuItemViewModel
                {
                    Header = _localizer["About"],
                    Icon = "\uE946", // Info icon
                    Command = new RelayCommand(ShowAboutDialog)
                }
            }
        }
    };
}

private void CreateNewItem()
{
    // Implementation for creating a new item
}

private void ShowSettings()
{
    // Show plugin settings dialog
}

private void ShowAboutDialog()
{
    // Show an about dialog
}
```

### Custom Dialogs

You can create custom dialogs for your plugin:

```csharp
public async Task ShowCustomDialogAsync()
{
    var dialog = new CustomDialog
    {
        Title = _localizer["DialogTitle"],
        WindowStartupLocation = WindowStartupLocation.CenterOwner
    };
    
    // Set dialog content
    dialog.Content = new CustomDialogContent
    {
        DataContext = new CustomDialogViewModel(_localizer)
    };
    
    // Show dialog
    var mainWindow = _serviceProvider.GetService<IMainWindow>();
    await dialog.ShowDialog(mainWindow as Window);
}
```

With corresponding dialog content:

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             x:Class="MyPlugin.Views.CustomDialogContent">
    <StackPanel Margin="20" Width="400">
        <TextBlock Text="{Binding Message}" 
                   TextWrapping="Wrap"
                   Margin="0,0,0,20" />
                   
        <TextBox Text="{Binding Input}"
                 Watermark="{Binding InputPlaceholder}"
                 Margin="0,0,0,20" />
                 
        <StackPanel Orientation="Horizontal" 
                    HorizontalAlignment="Right" 
                    Spacing="10">
            <Button Content="{Binding CancelText}" 
                    Command="{Binding CancelCommand}" />
            <Button Content="{Binding ConfirmText}" 
                    Command="{Binding ConfirmCommand}"
                    Classes="accent" />
        </StackPanel>
    </StackPanel>
</UserControl>
```

### Navigation Integration

Plugins can control their display in the navigation menu by setting navigation properties in their metadata:

```csharp
public class MyPluginMeta : IPluginMeta
{
    // ...other properties...
    
    // Menu icon character (using Segoe MDL2 Assets font)
    public string? NavigationIcon => "\uE8A5";
    
    // Menu position: header (top), body (middle), or footer (bottom)
    public string? NavigationSection => "body";
    
    // Sort order, lower numbers appear first
    public int NavigationOrder => 100;
}

## Development & Packaging

### Creating a Plugin Project

1. **Using the Template to Create a Plugin Project**

Use the Modulus provided project template to create a new plugin project:

```powershell
dotnet new modulus-plugin -n MyCustomPlugin
```

This will create a plugin project with the basic structure, including:
- Plugin entry class
- Basic configuration files
- Language resource files
- Sample views

2. **Plugin Basic Structure**

Your plugin project should contain the following key components:

- **Plugin Entry Class**: A main class implementing the `IPlugin` interface
- **Plugin Metadata Class**: A class implementing the `IPluginMeta` interface
- **Service Extensions Class**: A class containing service registration extension methods
- **Views**: The plugin's user interface components
- **Language Resources**: Resources for multi-language support
- **Configuration File**: Configuration file for plugin settings

### Using the Nuke Build System for Packaging

Modulus provides a unified Nuke build system to simplify the plugin development and packaging process.

#### Packaging Sample Plugins

Package all sample plugins:
```powershell
# Package all sample plugins (both approaches are equivalent)
nuke plugin
nuke plugin --op all
```

Package a specific plugin:
```powershell
# Package a specific plugin, e.g. SimplePlugin
nuke plugin --op single --name SimplePlugin
```

#### Packaging Output

Packaged plugins will be output to the following locations:

- **Plugin Directory**: `artifacts/plugins/{PluginName}/`
- **Plugin ZIP**: `artifacts/plugins/{PluginName}.zip`

The packaging process will display a colorful build summary including successful and failed plugins. Packaging processes are independent, so a failure in one plugin won't affect others. If a plugin fails to compile or package, related output files will be automatically cleaned up.

#### Plugin Project Example

Here's an example implementation of a plugin entry class:

```csharp
public class MyPluginEntry : IPlugin
{
    private readonly MyPluginMeta _metadata = new();

    public IPluginMeta GetMetadata() => _metadata;

    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // Register plugin services
        services.AddMyPluginServices();
        
        // Can use configuration
        var myOption = configuration.GetSection("Settings:MySetting1").Value;
        services.Configure<MyOptions>(configuration.GetSection("Settings"));
    }

    public void Initialize(IServiceProvider provider)
    {
        // Get a logger
        var logger = provider.GetService<ILogger<MyPluginEntry>>();
        logger?.LogInformation("MyPlugin initialized");
        
        // Perform other initialization
        var myService = provider.GetRequiredService<IMyService>();
        myService.Initialize();
    }

    public object? GetMainView()
    {
        // Return main view
        return new Views.MyPluginView();
    }

    public object? GetMenu()
    {
        // Return menu items (if any)
        return new Views.MyPluginMenu();
    }
}

public class MyPluginMeta : IPluginMeta
{
    public string Name => "My Custom Plugin";
    public string Version => "1.0.0";
    public string Description => "My custom plugin description";
    public string Author => "Your Name";
    public string[]? Dependencies => null;
    public string ContractVersion => "2.0.0";
    public string? NavigationIcon => "\uE8A5"; // Using Segoe MDL2 icon
    public string? NavigationSection => "body";
    public int NavigationOrder => 100;
}
```

### Manual Packaging

If you don't use the Nuke build system, you can manually package plugins:

1. Build the plugin project:
   ```powershell
   dotnet build -c Release
   ```

2. Copy output files to a plugin directory:
   - Main assembly (DLL files)
   - Configuration file (pluginsettings.json)
   - Language resource files (lang.*.json)
   - Other dependencies

3. Create a ZIP file for distribution

### Plugin Installation

Developed or built plugins can be installed in these locations:

- **Windows**: `%USERPROFILE%\.modulus\plugins\`
- **macOS/Linux**: `~/.modulus/plugins/`

After installation, plugins will be automatically loaded the next time Modulus starts. If hot reload is enabled, plugins can also be loaded without restarting the application.

## Plugin Security & Isolation

Modulus uses .NET's `AssemblyLoadContext` to implement plugin isolation, ensuring plugins run in independent contexts. This provides the following security and isolation guarantees:

### Resource Isolation

Each plugin is loaded in its own `AssemblyLoadContext`, which means:

1. **Assembly Isolation**: Assemblies loaded by one plugin will not conflict with assemblies from other plugins or the main application
2. **Type Isolation**: Each plugin has its own type system, avoiding type conflicts
3. **Memory Isolation**: Plugin static data and resources are contained within their own context
4. **Unload Capability**: Plugins can be unloaded without affecting the main application or other plugins

### Security Considerations

While Modulus provides basic plugin isolation, you should be aware of the following security considerations when developing and using plugins:

1. **Permission Limitations**:
   - Plugins still run in the same process as the main application
   - Plugins have access to the same file system and network resources as the main application
   - Avoid loading untrusted plugins

2. **Resource Usage**:
   - Monitor plugin resource usage like memory and CPU usage
   - Consider implementing resource limitation mechanisms
   - Check performance impacts of plugins

3. **Exception Handling**:
   - Unhandled exceptions in plugins can affect the main application
   - Use try-catch blocks and proper exception logging
   - Consider running high-risk plugins in separate AppDomains or processes

### Isolation Best Practices

1. **Secure Communication**:
   - Use well-defined interfaces for communication between plugins and the main application
   - Avoid sharing mutable state
   - Use message passing rather than direct references

2. **Versioned Interfaces**:
   - Use versioned interface design (e.g., `IMyServiceV1`, `IMyServiceV2`)
   - Include version properties in interfaces
   - Provide backward compatibility

3. **Sandbox Execution**:
   - For high-risk plugins, consider implementing additional sandbox execution environments
   - Limit file system and network access
   - Implement timeout mechanisms and resource limits

## Advanced Features

### Inter-Plugin Communication

Plugins can communicate with other plugins or the main application through:

1. **Shared Service Interfaces**

   ```csharp
   // Define shared interface (in a shared assembly)
   public interface ISharedService
   {
       void DoSomething();
   }

   // Plugin A implements interface
   public class PluginAService : ISharedService
   {
       public void DoSomething() { /* implementation */ }
   }

   // Plugin A registers service
   public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
   {
       services.AddSingleton<ISharedService, PluginAService>();
   }

   // Plugin B uses service
   public void Initialize(IServiceProvider provider)
   {
       var sharedService = provider.GetService<ISharedService>();
       if (sharedService != null)
       {
           sharedService.DoSomething();
       }
   }
   ```

2. **Event Aggregator Pattern**

   ```csharp
   // Define events in shared assembly
   public class MyEvent
   {
       public string Message { get; set; }
   }

   // Define event aggregator interface
   public interface IEventAggregator
   {
       void Publish<TEvent>(TEvent eventData);
       void Subscribe<TEvent>(Action<TEvent> handler);
       void Unsubscribe<TEvent>(Action<TEvent> handler);
   }

   // Plugin A publishes events
   public class PluginA
   {
       private readonly IEventAggregator _eventAggregator;

       public PluginA(IEventAggregator eventAggregator)
       {
           _eventAggregator = eventAggregator;
       }

       public void DoSomething()
       {
           _eventAggregator.Publish(new MyEvent { Message = "Hello from Plugin A" });
       }
   }

   // Plugin B subscribes to events
   public class PluginB
   {
       public PluginB(IEventAggregator eventAggregator)
       {
           eventAggregator.Subscribe<MyEvent>(HandleEvent);
       }

       private void HandleEvent(MyEvent evt)
       {
           Console.WriteLine(evt.Message);
       }
   }
   ```

3. **Extension Point Pattern**

   Implement an extension point pattern to allow plugins to extend specific application features:

   ```csharp
   // Define extension point interface
   public interface IToolbarExtension
   {
       string Name { get; }
       Control GetToolbarItem();
       int Order { get; }
   }

   // Implement extension point in plugin
   public class MyToolbarExtension : IToolbarExtension
   {
       public string Name => "MyTool";
       public int Order => 10;
       
       public Control GetToolbarItem()
       {
           var button = new Button { Content = "My Tool" };
           button.Click += (s, e) => { /* perform action */ };
           return button;
       }
   }

   // Register extension point
   public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
   {
       services.AddSingleton<IToolbarExtension, MyToolbarExtension>();
   }

   // Collect all extension points in main application
   var toolbarExtensions = serviceProvider.GetServices<IToolbarExtension>()
       .OrderBy(x => x.Order);

   foreach (var extension in toolbarExtensions)
   {
       toolbar.Items.Add(extension.GetToolbarItem());
   }
   ```

### Plugin Hot Reloading

Modulus supports plugin hot reloading, allowing updates without restarting the application:

1. **Hot Reload-Friendly Plugin Design**

   - Properly implement resource cleanup
   - Avoid static state
   - Use events rather than long-term references
   
   ```csharp
   public class MyPlugin : IPlugin, IDisposable
   {
       private readonly CancellationTokenSource _cts = new();
       private readonly List<IDisposable> _subscriptions = new();
       
       // Plugin initialization
       public void Initialize(IServiceProvider provider)
       {
           // Subscribe to events, save subscription for later cancellation
           var eventAggregator = provider.GetService<IEventAggregator>();
           var subscription = eventAggregator.Subscribe<MyEvent>(HandleEvent);
           _subscriptions.Add(subscription);
           
           // Start background tasks
           RunBackgroundTask(_cts.Token);
       }
       
       // Resource cleanup
       public void Dispose()
       {
           Dispose(true);
           GC.SuppressFinalize(this);
       }

       protected virtual void Dispose(bool disposing)
       {
           if (!_disposed)
           {
               if (disposing)
               {
                   _cts.Cancel();
                   _cts.Dispose();
                   // Dispose other managed resources
               }

               // Clean up unmanaged resources

               _disposed = true;
           }
       }
   }
   ```

2. **Handling Hot Reload Events**

   ```csharp
   // Listen for plugin reload events in the main application
   pluginLoader.PluginReloaded += (sender, args) =>
   {
       // Handle UI updates
       Application.Current.Dispatcher.Invoke(() =>
       {
           RefreshPluginViews();
       });
   };
   ```

### Extension Point Pattern

Implement an extension point pattern to allow plugins to extend specific application features:

```csharp
// Define extension point interface
public interface IToolbarExtension
{
    string Name { get; }
    Control GetToolbarItem();
    int Order { get; }
}

// Implement extension point in plugin
public class MyToolbarExtension : IToolbarExtension
{
    public string Name => "MyTool";
    public int Order => 10;
    
    public Control GetToolbarItem()
    {
        var button = new Button { Content = "My Tool" };
        button.Click += (s, e) => { /* perform action */ };
        return button;
    }
}

// Register extension point
public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
{
    services.AddSingleton<IToolbarExtension, MyToolbarExtension>();
}

// Collect all extension points in main application
var toolbarExtensions = serviceProvider.GetServices<IToolbarExtension>()
    .OrderBy(x => x.Order);

foreach (var extension in toolbarExtensions)
{
    toolbar.Items.Add(extension.GetToolbarItem());
}
```

### Plugin Dependency Management

Manage dependencies between plugins:

1. **Declaring Dependencies**

   ```csharp
   public class MyPluginMeta : IPluginMeta
   {
       // Other metadata...
       
       public string[]? Dependencies => new[] { "CorePlugin", "UILibraryPlugin" };
   }
   ```

2. **Checking Dependencies**

   ```csharp
   // Check dependencies before loading plugin in main program
   foreach (var dependency in pluginMeta.Dependencies)
   {
       if (!loadedPlugins.Any(p => p.GetMetadata().Name == dependency))
       {
           _logger.LogError($"Missing dependency: {dependency}");
           return false; // Skip loading
       }
   }
   ```

3. **Dependency Version Control**

   ```csharp
   // More advanced version dependency model
   public class PluginDependency
   {
       public string Name { get; set; }
       public string MinVersion { get; set; }
       public string MaxVersion { get; set; }
   }
   
   public class MyAdvancedPluginMeta : IPluginMeta
   {
       // Dependencies with version ranges
       public PluginDependency[] AdvancedDependencies => new[]
       {
           new PluginDependency 
           { 
               Name = "CorePlugin", 
               MinVersion = "1.0.0", 
               MaxVersion = "2.0.0" 
           }
       };
   }
   ```

## Best Practices

### Design & Architecture

1. **Follow Dependency Injection Principles**
   - Use constructor injection for dependencies, avoid static access and service locator patterns
   - Prefer interfaces over concrete implementation types
   - Use appropriate service registration lifetimes (Singleton, Scoped, Transient)

   ```csharp
   // Recommended
   public class MyService
   {
       private readonly ILogger<MyService> _logger;
       private readonly IDataService _dataService;

       public MyService(ILogger<MyService> logger, IDataService dataService)
       {
           _logger = logger;
           _dataService = dataService;
       }
   }

   // Avoid
   public class MyService
   {
       public void DoSomething()
       {
           var logger = ServiceLocator.GetService<ILogger>();
           var data = StaticDataAccess.GetData();
       }
   }
   ```

2. **Appropriate Isolation**
   - Layer your logic, separate UI, business logic, and data access
   - Use MVVM or similar architectural patterns for UI code
   - Create clear API boundaries

   ```
   MyPlugin/
   ├── Models/           # Data models
   ├── ViewModels/       # View models
   ├── Views/            # UI views
   ├── Services/         # Business logic services
   └── Data/             # Data access layer
   ```

3. **Compatibility Considerations**
   - Correctly declare the plugin's contract version in `ContractVersion`
   - Use conditional compilation (`#if` directives) to handle API differences between versions
   - Avoid using Modulus internal APIs, only use published interfaces

4. **Namespace Isolation**
   - Use unique namespace prefixes to avoid conflicts with other plugins or the main program
   - For example: `CompanyName.ProductName.PluginName`

### Coding Practices

1. **Exception Handling**
   - Catch and log exceptions, don't let them propagate to the main program
   - Provide meaningful error messages
   - Show user-friendly error messages in the UI layer

   ```csharp
   public void ProcessData()
   {
       try
       {
           // Business logic
       }
       catch (SpecificException ex)
       {
           _logger.LogError(ex, "Specific error handling");
           // Handle known exception
       }
       catch (Exception ex)
       {
           _logger.LogError(ex, "Error processing data");
           // Generic error handling
       }
   }
   ```

2. **Resource Disposal**
   - Properly implement resource disposal patterns, release all resources when the plugin is unloaded
   - Implement `IDisposable` interface (if needed)
   - Cancel long-running tasks
   - Close open connections and file handles

   ```csharp
   public class MyService : IDisposable
   {
       private readonly CancellationTokenSource _cts = new();
       private bool _disposed;

       public void Dispose()
       {
           Dispose(true);
           GC.SuppressFinalize(this);
       }

       protected virtual void Dispose(bool disposing)
       {
           if (!_disposed)
           {
               if (disposing)
               {
                   _cts.Cancel();
                   _cts.Dispose();
                   // Dispose other managed resources
               }

               // Clean up unmanaged resources

               _disposed = true;
           }
       }
   }
   ```

3. **Configuration Management**
   - Use strongly-typed configuration (via `IOptions<T>`) rather than directly accessing string key-values
   - Provide sensible defaults
   - Validate configuration values

   ```csharp
   // Register configuration
   services.Configure<MyPluginOptions>(configuration.GetSection("Settings"));

   // Use configuration
   public class MyService
   {
       private readonly MyPluginOptions _options;

       public MyService(IOptions<MyPluginOptions> options)
       {
           _options = options.Value;
       }
   }

   // Configuration class
   public class MyPluginOptions
   {
       public string Setting1 { get; set; } = "Default Value";
       public int Setting2 { get; set; } = 42;

       public bool IsValid()
       {
           return !string.IsNullOrEmpty(Setting1) && Setting2 > 0;
       }
   }
   ```

### Testing & Quality Assurance

1. **Write Unit Tests**
   - Write unit tests for core business logic
   - Use dependency injection to simplify testing
   - Use mocking frameworks (like Moq) to isolate dependencies

   ```csharp
   public void ProcessData_ValidInput_ReturnsExpectedResult()
   {
       // Arrange
       var loggerMock = new Mock<ILogger<MyService>>();
       var dataServiceMock = new Mock<IDataService>();
       dataServiceMock.Setup(x => x.GetData()).Returns(testData);
       
       var service = new MyService(loggerMock.Object, dataServiceMock.Object);
       
       // Act
       var result = service.ProcessData();
       
       // Assert
       Assert.Equal(expectedResult, result);
   }
   ```

2. **Integration Tests**
   - Test plugin behavior in the Modulus environment
   - Verify plugin loading, initialization, and unloading
   - Test integration with other components

3. **Logging**
   - Use appropriate log levels (Information, Warning, Error) at key points
   - Include context information, but avoid logging sensitive information
   - Use structured logging

## Troubleshooting Guide

### Common Issues & Solutions

1. **Plugin Cannot Load**

   **Possible Causes**:
   - Contract version incompatibility
   - Missing dependencies
   - Assembly conflicts

   **Solutions**:
   - Check if the plugin's `ContractVersion` is compatible with Modulus
   - Ensure all dependencies are included in the plugin directory
   - Check for assembly conflicts, try using different namespaces

2. **Services Cannot Resolve**

   **Possible Causes**:
   - Services not registered correctly
   - Dependency relationship issues
   - Scope issues

   **Solutions**:
   - Ensure services are registered correctly in the `ConfigureServices` method
   - Check dependency injection container configuration
   - Verify service lifetimes (Singleton, Scoped, Transient)

3. **Localization Not Working**

   **Possible Causes**:
   - Language file format errors
   - Language file path issues
   - Resource keys don't exist

   **Solutions**:
   - Ensure language files use the correct JSON format
   - Check if language files are named correctly (lang.en.json, lang.zh.json, etc.)
   - Verify that all language files contain the same resource keys

4. **Configuration Invalid**

   **Possible Causes**:
   - pluginsettings.json format errors
   - Configuration path issues
   - Missing default values

   **Solutions**:
   - Validate JSON format is correct
   - Check if configuration paths match those used in code
   - Provide sensible defaults for critical configurations

5. **UI Not Displaying**

   **Possible Causes**:
   - `GetMainView` method not implemented correctly
   - View creation failures
   - Navigation metadata issues

   **Solutions**:
   - Ensure `GetMainView` returns a valid UI control
   - Check for exceptions during view creation
   - Verify `NavigationIcon` and `NavigationSection` settings

6. **Packaging Errors**

   **Possible Causes**:
   - Project file structure issues
   - Missing necessary files
   - Build configuration errors

   **Solutions**:
   - Check project file structure
   - Ensure all necessary files are included correctly
   - Verify build configuration and dependencies

### Debugging Tips

1. **Enable Verbose Logging**

   Set log level to Debug or Trace:
   ```json
   {
     "Logging": {
       "LogLevel": {
         "Default": "Debug",
         "Modulus.PluginHost": "Trace"
       }
     }
   }
   ```

2. **Run in Debug Mode**

   Start Modulus in debug mode and set breakpoints in your plugin code.

3. **Check Plugin Loading Output**

   Modulus outputs plugin loading information at startup, check these logs to diagnose issues.

4. **Use Plugin Sandbox Mode**

   If possible, use Modulus's plugin sandbox mode for testing, which can prevent unstable plugins from crashing the entire application.

5. **Check Plugin Isolation Issues**

   Use reflection and load context debugging tools to check for assembly loading or type loading issues.

## Performance Optimization

Optimizing plugin performance is crucial for providing a smooth user experience:

### Loading Performance Optimization

1. **Minimize Dependencies**:
   - Include only necessary dependencies
   - Avoid large frameworks unless truly needed
   - Consider lazy loading techniques

2. **Resource Optimization**:
   - Compress images and other resources
   - Use appropriate image formats (e.g., SVG instead of high-resolution PNG)
   - Lazy load resources not immediately needed

3. **Initialization Optimization**:
   - Defer time-consuming initialization until necessary
   - Use asynchronous initialization patterns
   - Use priority queues to initialize most important components first

   ```csharp
   public async void Initialize(IServiceProvider provider)
   {
       // Initialize critical components immediately
       InitializeCore();
       
       // Initialize non-critical components asynchronously
       await Task.Run(() => InitializeNonCritical());
   }
   ```

### Runtime Performance Optimization

1. **UI Performance**:
   - Implement virtualization techniques (for long lists)
   - Avoid complex bindings and computed properties
   - Use appropriate caching strategies

2. **Memory Management**:
   - Avoid memory leaks
   - Properly release resources that are no longer needed
   - Implement object pooling patterns for frequently created/disposed objects

   ```csharp
   public class ObjectPool<T> where T : class, new()
   {
       private readonly ConcurrentBag<T> _objects = new();
       private readonly Func<T> _objectGenerator;

       public ObjectPool(Func<T> objectGenerator)
       {
           _objectGenerator = objectGenerator ?? (() => new T());
       }

       public T Get() => _objects.TryTake(out T? item) ? item : _objectGenerator();

       public void Return(T item) => _objects.Add(item);
   }
   ```

3. **Asynchronous Programming**:
   - Use asynchronous patterns for time-consuming operations
   - Avoid blocking the UI thread
   - Consider using Reactive Extensions for event streams

### Diagnostics & Monitoring

1. **Performance Profiling**:
   - Use profiling tools to identify bottlenecks
   - Monitor startup time and resource usage
   - Implement performance timing for critical operations

   ```csharp
   public void MeasureOperation()
   {
       var sw = Stopwatch.StartNew();
       
       // Perform operation
       PerformOperation();
       
       sw.Stop();
       _logger.LogDebug("Operation completed in {ElapsedMs}ms", sw.ElapsedMilliseconds);
   }
   ```

2. **Monitoring Framework**:
   - Consider implementing a plugin performance monitoring framework
   - Track plugin resource usage
   - Report performance issues to users

## Example Plugins

Reference these sample plugin implementations:

- **SimplePlugin**: Basic plugin example demonstrating minimal plugin implementation
- **ExamplePlugin**: Complete functionality demonstration including configuration, localization, services, and UI
- **NavigationExamplePlugin**: Navigation and UI extension example showing menu integration and navigation customization

These sample plugins can be found in the `src/samples/` directory.

## Advanced Plugin Patterns

### Plugin Communication

Plugins can communicate with each other and the host application through several patterns:

1. **Direct Service Dependency**:
   - Plugin A registers a service implementation
   - Plugin B depends on Plugin A and resolves the service

2. **Event Aggregator Pattern**:
   - Publishers emit events to a central event aggregator
   - Subscribers register to receive specific event types
   - Plugins remain decoupled but can still communicate

3. **Extension Points**:
   - The host application defines extension points
   - Plugins provide implementations for these extension points
   - The host discovers and uses these extensions

### Plugin Architecture Patterns

When designing complex plugins, consider these architectural patterns:

1. **Microkernel Architecture**:
   - Core plugin provides minimal functionality and robust extension API
   - Feature modules are loaded as needed through a separate plugin extension mechanism

2. **Modular Architecture**:
   - Break functionality into multiple logical modules
   - Each module is contained in a separate assembly but packaged as a single plugin
   - Use internal interfaces to communicate between modules

3. **Service-Oriented Architecture**:
   - Each plugin provides well-defined services
   - Services communicate via message passing
   - Promotes loose coupling and enhances testability

### Reactive Plugin Design

Reactive programming enhances the responsiveness of plugins:

1. **Reactive UI Updates**:
   - Use ReactiveUI to bind UI to observable data sources
   - UI updates automatically when data changes
   - Provides a responsive user experience

2. **Event Streams**:
   - Process event sequences using Reactive Extensions (Rx)
   - Filter, transform, and combine events
   - Process user input in a non-blocking way

3. **Reactive Communication**:
   - Observable data sources for cross-plugin communication
   - Subscribe to data changes from other plugins
   - Maintain loose coupling through observable patterns

## Reference Documentation

For detailed API documentation on all Modulus plugin interfaces and classes, please refer to the [Plugin API Reference](plugin-api-reference.md).

## Cross-Platform Considerations

When developing plugins for Modulus, keep in mind that the application supports multiple platforms:

1. **File Path Handling**:
   - Use cross-platform path methods: `Path.Combine()`, `Path.DirectorySeparatorChar`
   - Avoid hardcoded path separators

2. **Platform-Specific Features**:
   - Use conditional compilation to include platform-specific code
   - Check for platform capabilities before using them
   - Provide reasonable fallbacks for unsupported features

3. **UI Considerations**:
   - Test UI layouts on different screen sizes
   - Consider both mouse and touch input
   - Respect platform-specific UI guidelines

## Conclusion

Developing Modulus plugins is a powerful way to extend the application's functionality. This guide provides the foundation for developing high-quality, high-performance, and maintainable plugins.

By following best practices, well-organized directory structures, and clear code design, you can create plugins that integrate seamlessly into the Modulus ecosystem.

Remember to:

- **Focus on User Experience**: Create intuitive, responsive plugin interfaces
- **Maintain Maintainability**: Write clear, well-structured code
- **Respect the Platform**: Stay consistent with Modulus's design principles and UI style
- **Continuously Improve**: Gather user feedback and iterate on your plugins

We look forward to seeing the innovative solutions you create using the Modulus plugin system!

---

**Related Resources**:
- [Modulus API Documentation](https://docs.modulus.org/api)
- [Plugin Development Sample Repository](https://github.com/modulus/plugin-samples)
- [Developer Community Forum](https://community.modulus.org)
- [Video Tutorial: Developing Modulus Plugins from Scratch](https://learn.modulus.org/plugins)

## Visual Studio Code Integration

Modern development workflows often involve Visual Studio Code, and developing Modulus plugins is no exception. This section covers how to set up an optimal development environment for Modulus plugin development in VS Code.

### Recommended Extensions

For an optimal development experience, the following VS Code extensions are recommended:

1. **C# Extension (ms-dotnettools.csharp)**: Provides IntelliSense, debugging, and code navigation for C#
2. **Avalonia for VS Code (avaloniateam.vscode-avalonia)**: Provides XAML support for Avalonia UI
3. **XML Tools (dotjoshjohnson.xml)**: Enhanced XML editing experience
4. **.NET Core Test Explorer (formulahendry.dotnet-test-explorer)**: Visual test runner for .NET

### Setting Up the Development Environment

1. **Create a Launch Configuration**

   Create a `.vscode/launch.json` file in your project directory:

   ```json
   {
     "version": "0.2.0",
     "configurations": [
       {
         "name": "Debug Modulus Plugin",
         "type": "coreclr",
         "request": "launch",
         "preLaunchTask": "build",
         "program": "${workspaceFolder}/path/to/Modulus.App.Desktop.dll",
         "args": [],
         "cwd": "${workspaceFolder}",
         "stopAtEntry": false,
         "console": "internalConsole"
       }
     ]
   }
   ```

2. **Add a Tasks Configuration**

   Create a `.vscode/tasks.json` file:

   ```json
   {
     "version": "2.0.0",
     "tasks": [
       {
         "label": "build",
         "command": "dotnet",
         "type": "process",
         "args": [
           "build",
           "${workspaceFolder}/YourPlugin.csproj",
           "/property:GenerateFullPaths=true",
           "/consoleloggerparameters:NoSummary"
         ],
         "problemMatcher": "$msCompile"
       },
       {
         "label": "package",
         "command": "nuke",
         "type": "process",
         "args": [
           "plugin",
           "--op",
           "single",
           "--name",
           "YourPlugin"
         ],
         "problemMatcher": "$msCompile"
       }
     ]
   }
   ```

3. **Configure Intellisense**

   Create a `.vscode/settings.json` file:

   ```json
   {
     "omnisharp.enableRoslynAnalyzers": true,
     "omnisharp.enableEditorConfigSupport": true,
     "csharp.format.enable": true,
     "editor.formatOnSave": true
   }
   ```

### Debug with Hot Reload

VS Code supports hot reload for .NET applications, allowing you to modify your plugin while debugging:

1. **Enable Hot Reload**

   Add the following to your plugin project file:

   ```xml
   <PropertyGroup>
     <EnableHotReload>true</EnableHotReload>
   </PropertyGroup>
   ```

2. **Use XAML Hot Reload**

   When editing XAML files during a debug session, changes will be reflected immediately in the running application.

3. **Using Hot Reload with Modulus's Plugin System**

   Since Modulus has its own plugin hot-reloading system, you can combine both for an optimal development experience:

   ```csharp
   public class DevPlugin : IPlugin, IDisposable
   {
       // Standard plugin implementation
       
       // For development debugging
       [Conditional("DEBUG")]
       private void SetupHotReloadWatcher()
       {
           var watcher = new FileSystemWatcher(Directory.GetCurrentDirectory());
           watcher.Changed += (s, e) => {
               if (e.FullPath.EndsWith(".dll") || e.FullPath.EndsWith(".json"))
               {
                   // Signal Modulus to reload this plugin
                   OnPluginUpdated?.Invoke(this, EventArgs.Empty);
               }
           };
           watcher.EnableRaisingEvents = true;
       }
   }
   ```

## Advanced Security Practices

Security is a critical consideration when developing plugins that extend application functionality. This section covers advanced security practices for Modulus plugin development.

### Plugin Signing

While not required by default, plugin signing provides an additional layer of security and authenticity:

1. **Creating a Signing Certificate**

   ```powershell
   # Generate a self-signed certificate for development
   $cert = New-SelfSignedCertificate -Subject "CN=ModulusPluginDev" -Type CodeSigning -CertStoreLocation Cert:\CurrentUser\My
   
   # Export the certificate to a PFX file
   $password = ConvertTo-SecureString -String "YourPassword" -Force -AsPlainText
   Export-PfxCertificate -Cert $cert -FilePath "ModulusPluginDev.pfx" -Password $password
   ```

2. **Signing a Plugin Assembly**

   ```powershell
   # Sign the plugin assembly
   Set-AuthenticodeSignature -FilePath "path\to\YourPlugin.dll" -Certificate $cert
   ```

3. **Configuring Modulus to Verify Signatures**

   ```json
   {
     "PluginSecurity": {
       "RequireSignedPlugins": true,
       "TrustedPublishers": [
         "CN=ModulusPluginDev"
       ]
     }
   }
   ```

### Data Security

When handling sensitive data within your plugin:

1. **Secure Storage**

   Use the `SecureDataStorage` service provided by Modulus for storing sensitive information:

   ```csharp
   public class SecurePluginService
   {
       private readonly ISecureDataStorage _secureStorage;
       
       public SecurePluginService(ISecureDataStorage secureStorage)
       {
           _secureStorage = secureStorage;
       }
       
       public async Task SaveSecretAsync(string key, string secret)
       {
           await _secureStorage.SaveAsync(key, secret);
       }
       
       public async Task<string> RetrieveSecretAsync(string key)
       {
           return await _secureStorage.RetrieveAsync(key);
       }
   }
   ```

2. **Secure Communication**

   When communicating with external services, always use secure protocols:

   ```csharp
   public async Task<string> FetchDataSecurelyAsync(string url)
   {
       using var client = new HttpClient();
       // Always validate certificates in production
       client.DefaultRequestHeaders.Add("User-Agent", "Modulus Plugin");
       
       var response = await client.GetAsync(url);
       response.EnsureSuccessStatusCode();
       
       return await response.Content.ReadAsStringAsync();
   }
   ```

3. **Secure Configuration**

   Never store secrets in plain text configuration files:

   ```csharp
   // AVOID THIS:
   // {
   //   "ApiKey": "my-secret-api-key"
   // }
   
   // INSTEAD, use secure storage and ask for credentials when needed
   public async Task InitializeAsync()
   {
       if (!await _secureStorage.ExistsAsync("ApiKey"))
       {
           // Prompt user for API key if not stored
           var apiKey = await _dialogService.PromptForSecretAsync("Enter API Key");
           await _secureStorage.SaveAsync("ApiKey", apiKey);
       }
   }
   ```

### Code Security

Follow these practices to ensure your plugin code is secure:

1. **Input Validation**

   Always validate inputs, especially those coming from external sources:

   ```csharp
   public void ProcessUserInput(string input)
   {
       if (string.IsNullOrEmpty(input))
       {
           throw new ArgumentException("Input cannot be empty");
       }
       
       // Validate length
       if (input.Length > 1000)
       {
           throw new ArgumentException("Input too long");
       }
       
       // Validate format (e.g., using regex)
       if (!Regex.IsMatch(input, @"^[a-zA-Z0-9\s]+$"))
       {
           throw new ArgumentException("Input contains invalid characters");
       }
       
       // Process the validated input
       // ...
   }
   ```

2. **Safe Deserialization**

   Be cautious when deserializing data, especially from untrusted sources:

   ```csharp
   // Use safe deserialization options
   var options = new JsonSerializerOptions
   {
       MaxDepth = 10, // Prevent stack overflow attacks
       PropertyNameCaseInsensitive = true
   };
   
   try
   {
       var data = JsonSerializer.Deserialize<MyDataClass>(jsonString, options);
       // Process data
   }
   catch (JsonException ex)
   {
       _logger.LogError(ex, "Invalid JSON data");
       // Handle error
   }
   ```

3. **Principle of Least Privilege**

   Only request the permissions you need, and clearly document what your plugin does:

   ```csharp
   [PluginPermission("FileSystem", "Read only access to plugin directory")]
   [PluginPermission("Network", "Access to api.example.com")]
   public class MyPlugin : IPlugin
   {
       // Plugin implementation
   }
   ```

### Runtime Security Monitoring

Implement security monitoring in your plugin:

1. **Log Security Events**

   ```csharp
   public void ProcessUserAction(string userId, string action)
   {
       _logger.LogInformation("User {UserId} performed {Action}", userId, action);
       
       if (IsSensitiveAction(action))
       {
           _logger.LogWarning("Sensitive action {Action} performed by {UserId}", action, userId);
           // May also want to notify admins or implement additional verification
       }
   }
   ```

2. **Implement Rate Limiting**

   ```csharp
   private readonly Dictionary<string, (int Count, DateTime LastReset)> _requestCounts = new();
   private readonly object _lockObj = new();
   
   public bool CheckRateLimit(string clientId, int maxRequests = 100, int periodMinutes = 15)
   {
       lock (_lockObj)
       {
           if (!_requestCounts.TryGetValue(clientId, out var state))
           {
               state = (0, DateTime.UtcNow);
           }
           
           // Reset counter if period has elapsed
           if ((DateTime.UtcNow - state.LastReset).TotalMinutes >= periodMinutes)
           {
               state = (0, DateTime.UtcNow);
           }
           
           // Check if limit exceeded
           if (state.Count >= maxRequests)
           {
               return false; // Rate limit exceeded
           }
           
           // Update counter
           _requestCounts[clientId] = (state.Count + 1, state.LastReset);
           return true;
       }
   }
   ```

By implementing these advanced security practices, you can ensure your plugins not only function well but also maintain the security and integrity of the Modulus application and its users' data.

## Advanced Examples

To help you better understand advanced concepts and patterns in Modulus plugin development, we provide a dedicated [Advanced Examples](plugin-advanced-examples.md) document that includes:

- **Avalonia ReactiveUI Integration**: Create highly responsive UIs using reactive programming
- **Custom Plugin Settings UI**: Build sophisticated settings interfaces
- **Plugin Communication Patterns**: Implement inter-plugin communication using dependency resolution and event aggregators
- **Reactive Plugin Design Patterns**: Implement real-time data monitoring and reactive UI updates
- **Detailed Diagrams**: Visualize plugin lifecycle and communication patterns

These examples provide working code that you can directly integrate into your plugin projects, helping you master advanced plugin development techniques more quickly.