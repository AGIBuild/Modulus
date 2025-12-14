# Module Development Guide

This guide provides in-depth information about developing Modulus modules, including architecture, best practices, and advanced topics.

## Module Architecture

### Project Structure

A Modulus module consists of multiple projects:

```
MyModule/
├── MyModule.sln                    # Solution file
├── extension.vsixmanifest          # Module manifest
├── MyModule.Core/                  # Core logic (required)
│   ├── MyModule.Core.csproj
│   ├── MyModuleModule.cs           # Entry point
│   ├── ViewModels/
│   │   └── MainViewModel.cs
│   └── Services/
│       └── MyService.cs
├── MyModule.UI.Avalonia/           # Avalonia UI (optional)
│   ├── MyModule.UI.Avalonia.csproj
│   ├── MyModuleAvaloniaModule.cs
│   └── Views/
│       └── MainView.axaml
└── MyModule.UI.Blazor/             # Blazor UI (optional)
    ├── MyModule.UI.Blazor.csproj
    ├── MyModuleBlazorModule.cs
    └── Pages/
        └── MainView.razor
```

### Assembly Domain

Modules run in an isolated `AssemblyLoadContext` to prevent conflicts with other modules.

**Shared assemblies** (loaded once by host):
- `Modulus.Core`
- `Modulus.Sdk`
- `Modulus.UI.Abstractions`
- `Modulus.UI.Avalonia` / `Modulus.UI.Blazor`

**Module assemblies** (isolated per module):
- Your module DLLs
- Third-party dependencies

## Entry Point: ModulusPackage

Every module must have at least one class that extends `ModulusPackage`:

```csharp
using Modulus.Sdk;

namespace MyModule.Core;

public class MyModuleModule : ModulusPackage
{
    public override void ConfigureServices(IModuleLifecycleContext context)
    {
        // Register your services
        context.Services.AddSingleton<IMyService, MyService>();
        context.Services.AddTransient<MainViewModel>();
    }

    public override Task OnActivatedAsync(IModuleActivationContext context)
    {
        // Called when module is activated
        var logger = context.Services.GetRequiredService<ILogger<MyModuleModule>>();
        logger.LogInformation("MyModule activated!");
        return Task.CompletedTask;
    }

    public override Task OnDeactivatingAsync(IModuleDeactivationContext context)
    {
        // Called before module is deactivated
        // Clean up resources here
        return Task.CompletedTask;
    }
}
```

## Dependency Injection

Modulus uses Microsoft.Extensions.DependencyInjection. Register services in `ConfigureServices`:

```csharp
public override void ConfigureServices(IModuleLifecycleContext context)
{
    // Singleton - one instance for entire module lifetime
    context.Services.AddSingleton<IMyService, MyService>();
    
    // Scoped - one instance per scope (e.g., per page/view)
    context.Services.AddScoped<IDataContext, DataContext>();
    
    // Transient - new instance every time
    context.Services.AddTransient<MainViewModel>();
}
```

### Accessing Host Services

Host services are automatically available:

```csharp
public class MainViewModel
{
    private readonly INavigationService _navigation;
    private readonly IDialogService _dialog;
    private readonly ILogger<MainViewModel> _logger;

    public MainViewModel(
        INavigationService navigation,
        IDialogService dialog,
        ILogger<MainViewModel> logger)
    {
        _navigation = navigation;
        _dialog = dialog;
        _logger = logger;
    }
}
```

## ViewModel Pattern

### Base ViewModel

Use `CommunityToolkit.Mvvm` for MVVM support:

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MyModule.ViewModels;

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    private string _title = "My Module";

    [ObservableProperty]
    private string _message = "";

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        Message = "Loading...";
        // Load data
        Message = "Done!";
    }
}
```

### Navigation

Navigate between views using `INavigationService`:

```csharp
[RelayCommand]
private async Task NavigateToDetailsAsync()
{
    await _navigation.NavigateToAsync<DetailsViewModel>(new { Id = 123 });
}
```

## UI Development

### Avalonia Views

```xml
<!-- MainView.axaml -->
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:vm="using:MyModule.ViewModels"
             x:Class="MyModule.UI.Avalonia.MainView"
             x:DataType="vm:MainViewModel">
    
    <StackPanel Spacing="16" Margin="24">
        <TextBlock Text="{Binding Title}" 
                   Theme="{StaticResource TitleTextBlockStyle}" />
        
        <TextBox Text="{Binding Message}" 
                 Watermark="Enter message..." />
        
        <Button Content="Load Data" 
                Command="{Binding LoadDataCommand}" />
    </StackPanel>
</UserControl>
```

### Blazor Views

```razor
@* MainView.razor *@
@using MyModule.ViewModels
@inherits ModulusComponentBase<MainViewModel>

<div class="container">
    <h1>@ViewModel.Title</h1>
    
    <input @bind="ViewModel.Message" 
           placeholder="Enter message..." />
    
    <button @onclick="ViewModel.LoadDataCommand.Execute">
        Load Data
    </button>
</div>
```

### Registering UI Modules

For each UI platform, create a separate module class:

```csharp
// MyModuleAvaloniaModule.cs
using Modulus.UI.Avalonia;

namespace MyModule.UI.Avalonia;

public class MyModuleAvaloniaModule : ModulusPackage
{
    public override void ConfigureServices(IModuleLifecycleContext context)
    {
        // Register Avalonia-specific views
        context.Services.AddTransient<MainView>();
    }
}
```

## Module Manifest

### Basic Structure

```xml
<?xml version="1.0" encoding="utf-8"?>
<PackageManifest Version="2.0.0" 
    xmlns="http://schemas.microsoft.com/developer/vsx-schema/2011">
  
  <Metadata>
    <Identity Id="unique-guid" Version="1.0.0" Publisher="YourName" />
    <DisplayName>My Module</DisplayName>
    <Description>Module description</Description>
    <Tags>category, keywords</Tags>
  </Metadata>
  
  <Installation>
    <InstallationTarget Id="Modulus.Host.Avalonia" Version="[1.0,)" />
    <InstallationTarget Id="Modulus.Host.Blazor" Version="[1.0,)" />
  </Installation>
  
  <Assets>
    <!-- Core assembly (always loaded) -->
    <Asset Type="Modulus.Package" Path="MyModule.Core.dll" />
    
    <!-- UI assemblies (loaded based on host) -->
    <Asset Type="Modulus.Package" Path="MyModule.UI.Avalonia.dll" 
           TargetHost="Modulus.Host.Avalonia" />
    <Asset Type="Modulus.Package" Path="MyModule.UI.Blazor.dll" 
           TargetHost="Modulus.Host.Blazor" />
    
    <!-- Menu items -->
    <Asset Type="Modulus.Menu" 
           Id="mymodule-main" 
           DisplayName="My Module"
           Icon="Folder"
           Route="MyModule.ViewModels.MainViewModel"
           Location="Main"
           Order="100" />
  </Assets>
</PackageManifest>
```

### Menu Locations

| Location | Description |
|----------|-------------|
| `Main` | Main sidebar (default) |
| `Bottom` | Bottom of sidebar |
| `Settings` | Settings section |

### Multiple Menus

```xml
<Assets>
  <Asset Type="Modulus.Menu" Id="mymodule-main" 
         DisplayName="Dashboard" Icon="Home" 
         Route="MyModule.ViewModels.DashboardViewModel" 
         Location="Main" Order="10" />
         
  <Asset Type="Modulus.Menu" Id="mymodule-settings" 
         DisplayName="Settings" Icon="Settings" 
         Route="MyModule.ViewModels.SettingsViewModel" 
         Location="Bottom" Order="100" />
</Assets>
```

## Testing

### Unit Testing ViewModels

```csharp
[Fact]
public async Task LoadData_ShouldUpdateMessage()
{
    // Arrange
    var vm = new MainViewModel();
    
    // Act
    await vm.LoadDataCommand.ExecuteAsync(null);
    
    // Assert
    Assert.Equal("Done!", vm.Message);
}
```

### Integration Testing

```csharp
[Fact]
public async Task Module_ShouldLoadSuccessfully()
{
    // Arrange
    var services = new ServiceCollection();
    var context = new TestModuleLifecycleContext(services);
    var module = new MyModuleModule();
    
    // Act
    module.ConfigureServices(context);
    var provider = services.BuildServiceProvider();
    
    // Assert
    var service = provider.GetService<IMyService>();
    Assert.NotNull(service);
}
```

## Best Practices

### 1. Keep Core Host-Agnostic

Don't reference Avalonia or Blazor in your Core project:

```csharp
// ❌ Wrong - in Core project
using Avalonia.Controls;

// ✅ Correct - use abstractions
using Modulus.UI.Abstractions;
```

### 2. Use Async/Await

```csharp
// ❌ Wrong
public void LoadData()
{
    var data = _service.GetData().Result; // Blocks thread
}

// ✅ Correct
public async Task LoadDataAsync()
{
    var data = await _service.GetDataAsync();
}
```

### 3. Dispose Resources

```csharp
public override async Task OnDeactivatingAsync(IModuleDeactivationContext context)
{
    // Clean up subscriptions, timers, etc.
    _subscription?.Dispose();
    await _database.CloseAsync();
}
```

### 4. Handle Errors Gracefully

```csharp
[RelayCommand]
private async Task LoadDataAsync()
{
    try
    {
        Data = await _service.GetDataAsync();
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to load data");
        await _dialog.ShowErrorAsync("Failed to load data", ex.Message);
    }
}
```

## Debugging

### Running with Debugger

1. Set the Host project as startup project
2. Set breakpoints in your module code
3. Run with debugger (F5)

### Logging

```csharp
public class MyService
{
    private readonly ILogger<MyService> _logger;

    public MyService(ILogger<MyService> logger)
    {
        _logger = logger;
    }

    public async Task DoWorkAsync()
    {
        _logger.LogInformation("Starting work...");
        _logger.LogDebug("Details: {Details}", someDetails);
        _logger.LogWarning("Something unusual: {Issue}", issue);
        _logger.LogError(exception, "Work failed");
    }
}
```

## Distribution

### Package Version

Version is read from the manifest `Identity/@Version`:

```xml
<Identity Id="mymodule-id" Version="1.2.3" Publisher="Acme" />
```

### Publishing

```bash
# Package
modulus pack

# The .modpkg file can be distributed via:
# - Direct download
# - GitHub Releases
# - (Future) Modulus Module Store
```

## See Also

- [Getting Started](./getting-started.md)
- [CLI Reference](./cli-reference.md)
- [Manifest Format](./manifest-format.md)
- [UI Components](./ui-components.md)

