# Modulus Plugin Development - Advanced Examples

This document contains advanced examples and patterns for Modulus plugin development that complements the main [Plugin Development Guide](plugin-development-guide.md).

## Avalonia ReactiveUI Integration

The following example demonstrates how to integrate with Avalonia ReactiveUI to create a reactive plugin with proper MVVM architecture:

```csharp
// ViewModels/ReactiveViewModel.cs
using ReactiveUI;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows.Input;
using Modulus.Plugin.Abstractions;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace AdvancedPlugin.ViewModels
{
    public class ReactiveViewModel : ReactiveObject, IActivatableViewModel
    {
        private readonly IPluginService _service;
        private string _searchText = string.Empty;
        private ObservableCollection<SearchResult> _results = new();
        private bool _isLoading;

        public ReactiveViewModel(IPluginService service)
        {
            _service = service;
            
            // Create activation for this view model
            Activator = new ViewModelActivator();
            
            // Setup command
            SearchCommand = ReactiveCommand.CreateFromTask(
                async () => await PerformSearch(), 
                this.WhenAnyValue(x => x.SearchText, text => !string.IsNullOrEmpty(text))
            );
            
            // Handle activation/deactivation
            this.WhenActivated((CompositeDisposable disposables) =>
            {
                // Execute startup logic when ViewModel is activated
                
                // Clean up when the ViewModel is deactivated
                Disposable
                    .Create(() => _service.CancelPendingOperations())
                    .DisposeWith(disposables);
            });
        }

        public ViewModelActivator Activator { get; }
        
        public string SearchText 
        { 
            get => _searchText;
            set => this.RaiseAndSetIfChanged(ref _searchText, value);
        }
        
        public ObservableCollection<SearchResult> Results 
        { 
            get => _results;
            private set => this.RaiseAndSetIfChanged(ref _results, value);
        }
        
        public bool IsLoading 
        { 
            get => _isLoading;
            private set => this.RaiseAndSetIfChanged(ref _isLoading, value);
        }
        
        public ICommand SearchCommand { get; }
        
        private async Task PerformSearch()
        {
            try
            {
                IsLoading = true;
                var results = await _service.SearchAsync(SearchText);
                Results = new ObservableCollection<SearchResult>(results);
            }
            catch (Exception ex)
            {
                // Handle exceptions
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}
```

```xml
<!-- Views/ReactiveView.axaml -->
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             xmlns:vm="using:AdvancedPlugin.ViewModels"
             mc:Ignorable="d" d:DesignWidth="800" d:DesignHeight="450"
             x:Class="AdvancedPlugin.Views.ReactiveView">
  
  <Grid RowDefinitions="Auto,*,Auto">
    <!-- Search Box -->
    <Grid Grid.Row="0" ColumnDefinitions="*,Auto" Margin="20">
      <TextBox Grid.Column="0" 
               Text="{Binding SearchText}" 
               Watermark="Enter search terms..." />
      <Button Grid.Column="1" 
              Content="Search" 
              Command="{Binding SearchCommand}"
              Margin="10,0,0,0" />
    </Grid>
    
    <!-- Results List -->
    <ListBox Grid.Row="1" 
             Items="{Binding Results}"
             Margin="20,0">
      <ListBox.ItemTemplate>
        <DataTemplate>
          <StackPanel Margin="5">
            <TextBlock Text="{Binding Title}" FontWeight="Bold" />
            <TextBlock Text="{Binding Description}" TextWrapping="Wrap" />
          </StackPanel>
        </DataTemplate>
      </ListBox.ItemTemplate>
    </ListBox>
    
    <!-- Status Bar -->
    <StackPanel Grid.Row="2" 
                Orientation="Horizontal"
                HorizontalAlignment="Right"
                Margin="20">
      <ProgressBar IsIndeterminate="True" 
                   IsVisible="{Binding IsLoading}"
                   Width="100"
                   Height="10"
                   Margin="0,0,10,0" />
      <TextBlock Text="Loading..." 
                 IsVisible="{Binding IsLoading}" />
    </StackPanel>
  </Grid>
</UserControl>
```

## Custom Plugin Settings UI

This example shows how to create a custom settings page for your plugin:

```csharp
// Models/PluginSettings.cs
public class PluginSettings
{
    public string ApiKey { get; set; } = string.Empty;
    public bool EnableFeatureX { get; set; } = true;
    public int CacheTimeout { get; set; } = 300;
    public List<string> EnabledProfiles { get; set; } = new();
}

// ViewModels/SettingsViewModel.cs
public class SettingsViewModel : ViewModelBase
{
    private readonly IPluginSettings _pluginSettings;
    private readonly ILocalizer _localizer;
    private PluginSettings _settings;
    
    public SettingsViewModel(IPluginSettings pluginSettings, ILocalizer localizer)
    {
        _pluginSettings = pluginSettings;
        _localizer = localizer;
        
        // Load settings from configuration
        _settings = LoadSettings();
        
        SaveCommand = ReactiveCommand.Create(SaveSettings);
    }
    
    public string ApiKey
    {
        get => _settings.ApiKey;
        set
        {
            if (_settings.ApiKey != value)
            {
                _settings.ApiKey = value;
                this.RaisePropertyChanged();
            }
        }
    }
    
    public bool EnableFeatureX
    {
        get => _settings.EnableFeatureX;
        set
        {
            if (_settings.EnableFeatureX != value)
            {
                _settings.EnableFeatureX = value;
                this.RaisePropertyChanged();
            }
        }
    }
    
    public int CacheTimeout
    {
        get => _settings.CacheTimeout;
        set
        {
            if (_settings.CacheTimeout != value)
            {
                _settings.CacheTimeout = value;
                this.RaisePropertyChanged();
            }
        }
    }
    
    public ICommand SaveCommand { get; }
    
    private PluginSettings LoadSettings()
    {
        var config = _pluginSettings.Configuration;
        var settings = new PluginSettings
        {
            ApiKey = config["Settings:ApiKey"] ?? string.Empty,
            EnableFeatureX = config.GetValue<bool>("Settings:EnableFeatureX", true),
            CacheTimeout = config.GetValue<int>("Settings:CacheTimeout", 300)
        };
        
        var enabledProfilesString = config["Settings:EnabledProfiles"];
        if (!string.IsNullOrEmpty(enabledProfilesString))
        {
            settings.EnabledProfiles = enabledProfilesString.Split(',').ToList();
        }
        
        return settings;
    }
    
    private void SaveSettings()
    {
        // In a real implementation, you would save settings to a configuration file
        // This requires access to write the configuration file
        
        // For demonstration purposes, we'll just log that settings were saved
        var logger = Program.ServiceProvider.GetService<ILogger<SettingsViewModel>>();
        logger?.LogInformation("Settings saved: API Key={ApiKeyLength}, FeatureX={EnableFeatureX}, CacheTimeout={CacheTimeout}",
            ApiKey.Length, EnableFeatureX, CacheTimeout);
    }
}
```

```xml
<!-- Views/SettingsView.axaml -->
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             xmlns:vm="using:MyPlugin.ViewModels"
             mc:Ignorable="d" d:DesignWidth="800" d:DesignHeight="450"
             x:Class="MyPlugin.Views.SettingsView">

  <ScrollViewer>
    <StackPanel Margin="20" Spacing="15">
      <TextBlock Text="Plugin Settings" 
                 FontSize="24" 
                 FontWeight="Bold" 
                 Margin="0,0,0,10" />
      
      <!-- API Key Setting -->
      <StackPanel>
        <TextBlock Text="API Key" FontWeight="SemiBold" />
        <TextBox Text="{Binding ApiKey}" 
                 PasswordChar="•" 
                 Watermark="Enter your API key"
                 Margin="0,5,0,0" />
      </StackPanel>
      
      <!-- Feature Toggle -->
      <StackPanel>
        <TextBlock Text="Features" FontWeight="SemiBold" />
        <CheckBox Content="Enable Feature X" 
                  IsChecked="{Binding EnableFeatureX}"
                  Margin="0,5,0,0" />
      </StackPanel>
      
      <!-- Numeric Setting -->
      <StackPanel>
        <TextBlock Text="Cache Timeout (seconds)" FontWeight="SemiBold" />
        <NumericUpDown Value="{Binding CacheTimeout}" 
                       Minimum="0"
                       Maximum="3600"
                       Increment="60"
                       Margin="0,5,0,0" />
      </StackPanel>
      
      <!-- Save Button -->
      <Button Content="Save Settings"
              Command="{Binding SaveCommand}"
              HorizontalAlignment="Right"
              Margin="0,20,0,0" />
    </StackPanel>
  </ScrollViewer>
</UserControl>
```

## Plugin Communication Patterns

### Plugin Interoperability with Dependency Resolution

This example demonstrates how one plugin can depend on another through a shared interface:

```csharp
// In SharedInterfaces.dll (separate shared assembly)
namespace Modulus.Shared
{
    public interface IDataProvider
    {
        Task<IEnumerable<DataItem>> GetDataAsync();
    }
    
    public class DataItem
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}

// In ProviderPlugin project
public class ProviderPluginEntry : IPlugin
{
    private readonly ProviderPluginMeta _metadata = new();
    
    public IPluginMeta GetMetadata() => _metadata;
    
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // Register the provider implementation
        services.AddSingleton<Modulus.Shared.IDataProvider, DefaultDataProvider>();
    }
    
    // Other IPlugin implementation methods...
}

// In ConsumerPlugin project
public class ConsumerPluginEntry : IPlugin
{
    private readonly ConsumerPluginMeta _metadata = new();
    
    public IPluginMeta GetMetadata() => _metadata;
    
    public class ConsumerPluginMeta : IPluginMeta
    {
        // Other metadata properties...
        
        // Declare dependency on ProviderPlugin
        public string[]? Dependencies => new[] { "ProviderPlugin" };
    }
    
    public void Initialize(IServiceProvider provider)
    {
        // Resolve the provider from the dependency
        var dataProvider = provider.GetService<Modulus.Shared.IDataProvider>();
        if (dataProvider == null)
        {
            var logger = provider.GetService<ILogger<ConsumerPluginEntry>>();
            logger?.LogError("Failed to resolve IDataProvider. ProviderPlugin may not be loaded correctly.");
        }
        else
        {
            // Successfully resolved the provider
        }
    }
    
    // Other IPlugin implementation methods...
}
```

### Event Aggregator Pattern

```csharp
// Event aggregator interface
public interface IEventAggregator
{
    void Publish<TEvent>(TEvent eventData);
    void Subscribe<TEvent>(Action<TEvent> handler);
    void Unsubscribe<TEvent>(Action<TEvent> handler);
}

// Simple implementation
public class EventAggregator : IEventAggregator
{
    private readonly Dictionary<Type, List<object>> _subscribers = new Dictionary<Type, List<object>>();
    private readonly object _lock = new object();

    public void Publish<TEvent>(TEvent eventData)
    {
        if (eventData == null) throw new ArgumentNullException(nameof(eventData));

        List<object> handlers;
        lock (_lock)
        {
            if (!_subscribers.TryGetValue(typeof(TEvent), out handlers))
                return;
        }

        // Take a snapshot of handlers to avoid issues if collection changes during iteration
        var handlersToNotify = handlers.ToList();
        foreach (var handler in handlersToNotify)
        {
            ((Action<TEvent>)handler)(eventData);
        }
    }

    public void Subscribe<TEvent>(Action<TEvent> handler)
    {
        if (handler == null) throw new ArgumentNullException(nameof(handler));

        lock (_lock)
        {
            if (!_subscribers.TryGetValue(typeof(TEvent), out var handlers))
            {
                handlers = new List<object>();
                _subscribers[typeof(TEvent)] = handlers;
            }

            if (!handlers.Contains(handler))
            {
                handlers.Add(handler);
            }
        }
    }

    public void Unsubscribe<TEvent>(Action<TEvent> handler)
    {
        if (handler == null) throw new ArgumentNullException(nameof(handler));

        lock (_lock)
        {
            if (_subscribers.TryGetValue(typeof(TEvent), out var handlers))
            {
                handlers.Remove(handler);
            }
        }
    }
}

// Register in plugin
public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
{
    // Register event aggregator as singleton
    services.AddSingleton<IEventAggregator, EventAggregator>();
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
        // Publish an event that other plugins can listen for
        _eventAggregator.Publish(new DataUpdatedEvent 
        { 
            Source = "PluginA",
            Timestamp = DateTime.UtcNow
        });
    }
}

// Plugin B subscribes to events
public class PluginB
{
    private readonly ILogger<PluginB> _logger;
    private readonly IEventAggregator _eventAggregator;

    public PluginB(ILogger<PluginB> logger, IEventAggregator eventAggregator)
    {
        _logger = logger;
        _eventAggregator = eventAggregator;
        
        // Subscribe to events
        _eventAggregator.Subscribe<DataUpdatedEvent>(HandleDataUpdated);
    }

    private void HandleDataUpdated(DataUpdatedEvent evt)
    {
        _logger.LogInformation("Received data update from {Source} at {Timestamp}", 
            evt.Source, evt.Timestamp);
        
        // React to the event
    }
    
    // Don't forget to unsubscribe when plugin is unloaded
    public void Dispose()
    {
        _eventAggregator.Unsubscribe<DataUpdatedEvent>(HandleDataUpdated);
    }
}

// Event class
public class DataUpdatedEvent
{
    public string Source { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}
```

## Reactive Plugin Design Patterns

### Reactive UI Updates

```csharp
// Real-time data monitoring view model
public class MonitorViewModel : ReactiveObject
{
    private readonly IDataMonitorService _dataService;
    private readonly ObservableAsPropertyHelper<double> _currentValue;
    private readonly ObservableAsPropertyHelper<List<DataPoint>> _dataPoints;
    private readonly ObservableAsPropertyHelper<bool> _isConnected;

    public MonitorViewModel(IDataMonitorService dataService)
    {
        _dataService = dataService;
        
        // Transform the service's real-time data to view properties
        _currentValue = _dataService.CurrentValueStream
            .ToProperty(this, x => x.CurrentValue);
            
        _dataPoints = _dataService.DataStream
            .Scan(new List<DataPoint>(), (list, newPoint) => {
                var newList = new List<DataPoint>(list) { newPoint };
                // Keep only the last 100 points
                return newList.Skip(Math.Max(0, newList.Count - 100)).ToList();
            })
            .ToProperty(this, x => x.DataPoints);
            
        _isConnected = _dataService.ConnectionStatusStream
            .ToProperty(this, x => x.IsConnected);
            
        // Commands
        ConnectCommand = ReactiveCommand.CreateFromTask(
            async () => await _dataService.ConnectAsync());
            
        DisconnectCommand = ReactiveCommand.Create(
            () => _dataService.Disconnect());
    }
    
    // Read-only properties backed by real-time streams
    public double CurrentValue => _currentValue.Value;
    public List<DataPoint> DataPoints => _dataPoints.Value;
    public bool IsConnected => _isConnected.Value;
    
    public ICommand ConnectCommand { get; }
    public ICommand DisconnectCommand { get; }
    
    // Clean up resources
    public void Dispose()
    {
        _currentValue?.Dispose();
        _dataPoints?.Dispose();
        _isConnected?.Dispose();
    }
}
```

## Diagrams

### Plugin Lifecycle Diagram

```
┌─────────────────────────────────────────────────────────────────────────┐
│                         Plugin Lifecycle                                │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  ┌─────────┐      ┌────────────┐      ┌────────────┐     ┌───────────┐  │
│  │Discovery│ ──▶ │ Validation │ ──▶ │Registration│ ──▶ │Activation │  │
│  └─────────┘      └────────────┘      └────────────┘     └───────────┘  │
│       │                 ▲                   │                 │         │
│       │                 │                   │                 │         │
│       ▼                 │                   ▼                 ▼         │
│  ┌──────────────┐       │           ┌─────────────┐    ┌─────────────┐  │
│  │ Plugin Files │       │           │ DI Services │    │    UI       │  │
│  │ - DLLs       │       │           │ Registration│    │ Integration │  │
│  │ - Config     │       │           └─────────────┘    └─────────────┘  │
│  │ - Resources  │       │                                    │         │
│  └──────────────┘       │                                    │         │
│                         │                                    │         │
│  ┌─────────────┐   ┌────────────┐    ┌───────────────┐       │         │
│  │  Dispose   │ ◀─┤  Unloading │ ◀──┤ De-activation │ ◀─────┘         │
│  └─────────────┘   └────────────┘    └───────────────┘                 │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

### Plugin Communication Patterns Diagram

```
┌───────────────────────────────────────────────────────────────────────────┐
│                         Plugin Communication Patterns                      │
├───────────────────────────────────────────────────────────────────────────┤
│                                                                           │
│  ┌─────────────────┐                            ┌─────────────────┐       │
│  │    Plugin A     │                            │    Plugin B     │       │
│  ├─────────────────┤                            ├─────────────────┤       │
│  │                 │                            │                 │       │
│  │ ┌─────────────┐ │        Pattern 1           │ ┌─────────────┐ │       │
│  │ │  Service    │ │       Direct DI            │ │  Service    │ │       │
│  │ │  Provider   │ │ ────────────────────────▶ │ │  Consumer   │ │       │
│  │ └─────────────┘ │                            │ └─────────────┘ │       │
│  │                 │                            │                 │       │
│  │ ┌─────────────┐ │        Pattern 2           │ ┌─────────────┐ │       │
│  │ │   Event     │ │ ◀────────────────────────▶ │ │   Event     │ │       │
│  │ │  Publisher  │ │     Event Aggregator       │ │ Subscriber  │ │       │
│  │ └─────────────┘ │                            │ └─────────────┘ │       │
│  │                 │                            │                 │       │
│  └─────────────────┘                            └─────────────────┘       │
│          ▲                                               ▲                │
│          │                                               │                │
│          │                                               │                │
│          │                Pattern 3                      │                │
│          └───────────────────────────────────────────────┘                │
│                         Shared Services                                   │
│                                                                           │
│  ┌─────────────────────────────────────────────────────────────────┐      │
│  │                      Host Application                            │      │
│  │ ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐   │      │
│  │ │ Event Aggregator│  │ Service Registry│  │ Extension Points│   │      │
│  │ └─────────────────┘  └─────────────────┘  └─────────────────┘   │      │
│  └─────────────────────────────────────────────────────────────────┘      │
│                                                                           │
└───────────────────────────────────────────────────────────────────────────┘
```

## References and Further Reading

- [Plugin Development Guide](plugin-development-guide.md) - Main plugin development documentation
- [Plugin API Reference](plugin-api-reference.md) - Detailed API documentation
- [ReactiveUI Documentation](https://www.reactiveui.net/docs/) - For reactive UI patterns
- [Avalonia UI Documentation](https://docs.avaloniaui.net/) - For UI development
