# Modulus 插件开发 - 高级示例

本文档包含 Modulus 插件开发的高级示例和模式，作为主要[插件开发指南](plugin-development-guide.md)的补充。

## Avalonia ReactiveUI 集成

以下示例演示了如何与 Avalonia ReactiveUI 集成，以创建具有适当 MVVM 架构的响应式插件：

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
            
            // 为这个视图模型创建激活器
            Activator = new ViewModelActivator();
            
            // 设置命令
            SearchCommand = ReactiveCommand.CreateFromTask(
                async () => await PerformSearch(), 
                this.WhenAnyValue(x => x.SearchText, text => !string.IsNullOrEmpty(text))
            );
            
            // 处理激活/停用
            this.WhenActivated((CompositeDisposable disposables) =>
            {
                // 当视图模型被激活时执行启动逻辑
                
                // 当视图模型被停用时进行清理
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
                // 处理异常
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
    <!-- 搜索框 -->
    <Grid Grid.Row="0" ColumnDefinitions="*,Auto" Margin="20">
      <TextBox Grid.Column="0" 
               Text="{Binding SearchText}" 
               Watermark="输入搜索词..." />
      <Button Grid.Column="1" 
              Content="搜索" 
              Command="{Binding SearchCommand}"
              Margin="10,0,0,0" />
    </Grid>
    
    <!-- 结果列表 -->
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
    
    <!-- 状态栏 -->
    <StackPanel Grid.Row="2" 
                Orientation="Horizontal"
                HorizontalAlignment="Right"
                Margin="20">
      <ProgressBar IsIndeterminate="True" 
                   IsVisible="{Binding IsLoading}"
                   Width="100"
                   Height="10"
                   Margin="0,0,10,0" />
      <TextBlock Text="加载中..." 
                 IsVisible="{Binding IsLoading}" />
    </StackPanel>
  </Grid>
</UserControl>
```

## 自定义插件设置 UI

此示例显示了如何为插件创建自定义设置页面：

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
        
        // 从配置中加载设置
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
        // 在实际实现中，您需要将设置保存到配置文件
        // 这需要有写入配置文件的权限
        
        // 为演示目的，我们只是记录设置已保存
        var logger = Program.ServiceProvider.GetService<ILogger<SettingsViewModel>>();
        logger?.LogInformation("设置已保存: API Key={ApiKeyLength}, 功能X={EnableFeatureX}, 缓存超时={CacheTimeout}",
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
      <TextBlock Text="插件设置" 
                 FontSize="24" 
                 FontWeight="Bold" 
                 Margin="0,0,0,10" />
      
      <!-- API 密钥设置 -->
      <StackPanel>
        <TextBlock Text="API 密钥" FontWeight="SemiBold" />
        <TextBox Text="{Binding ApiKey}" 
                 PasswordChar="•" 
                 Watermark="输入您的 API 密钥"
                 Margin="0,5,0,0" />
      </StackPanel>
      
      <!-- 功能开关 -->
      <StackPanel>
        <TextBlock Text="功能" FontWeight="SemiBold" />
        <CheckBox Content="启用功能 X" 
                  IsChecked="{Binding EnableFeatureX}"
                  Margin="0,5,0,0" />
      </StackPanel>
      
      <!-- 数值设置 -->
      <StackPanel>
        <TextBlock Text="缓存超时 (秒)" FontWeight="SemiBold" />
        <NumericUpDown Value="{Binding CacheTimeout}" 
                       Minimum="0"
                       Maximum="3600"
                       Increment="60"
                       Margin="0,5,0,0" />
      </StackPanel>
      
      <!-- 保存按钮 -->
      <Button Content="保存设置"
              Command="{Binding SaveCommand}"
              HorizontalAlignment="Right"
              Margin="0,20,0,0" />
    </StackPanel>
  </ScrollViewer>
</UserControl>
```

## 插件通信模式

### 使用依赖解析的插件互操作性

此示例演示如何通过共享接口使一个插件依赖于另一个插件：

```csharp
// 在 SharedInterfaces.dll 中（单独的共享程序集）
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

// 在 ProviderPlugin 项目中
public class ProviderPluginEntry : IPlugin
{
    private readonly ProviderPluginMeta _metadata = new();
    
    public IPluginMeta GetMetadata() => _metadata;
    
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // 注册提供者实现
        services.AddSingleton<Modulus.Shared.IDataProvider, DefaultDataProvider>();
    }
    
    // 其他 IPlugin 实现方法...
}

// 在 ConsumerPlugin 项目中
public class ConsumerPluginEntry : IPlugin
{
    private readonly ConsumerPluginMeta _metadata = new();
    
    public IPluginMeta GetMetadata() => _metadata;
    
    public class ConsumerPluginMeta : IPluginMeta
    {
        // 其他元数据属性...
        
        // 声明对 ProviderPlugin 的依赖
        public string[]? Dependencies => new[] { "ProviderPlugin" };
    }
    
    public void Initialize(IServiceProvider provider)
    {
        // 从依赖中解析提供者
        var dataProvider = provider.GetService<Modulus.Shared.IDataProvider>();
        if (dataProvider == null)
        {
            var logger = provider.GetService<ILogger<ConsumerPluginEntry>>();
            logger?.LogError("无法解析 IDataProvider。ProviderPlugin 可能未正确加载。");
        }
        else
        {
            // 成功解析提供者
        }
    }
    
    // 其他 IPlugin 实现方法...
}
```

### 事件聚合器模式

```csharp
// 事件聚合器接口
public interface IEventAggregator
{
    void Publish<TEvent>(TEvent eventData);
    void Subscribe<TEvent>(Action<TEvent> handler);
    void Unsubscribe<TEvent>(Action<TEvent> handler);
}

// 简单实现
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

        // 获取处理程序的快照以避免在迭代期间集合变化的问题
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

// 在插件中注册
public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
{
    // 将事件聚合器注册为单例
    services.AddSingleton<IEventAggregator, EventAggregator>();
}

// 插件 A 发布事件
public class PluginA
{
    private readonly IEventAggregator _eventAggregator;

    public PluginA(IEventAggregator eventAggregator)
    {
        _eventAggregator = eventAggregator;
    }

    public void DoSomething()
    {
        // 发布其他插件可以监听的事件
        _eventAggregator.Publish(new DataUpdatedEvent 
        { 
            Source = "PluginA",
            Timestamp = DateTime.UtcNow
        });
    }
}

// 插件 B 订阅事件
public class PluginB
{
    private readonly ILogger<PluginB> _logger;
    private readonly IEventAggregator _eventAggregator;

    public PluginB(ILogger<PluginB> logger, IEventAggregator eventAggregator)
    {
        _logger = logger;
        _eventAggregator = eventAggregator;
        
        // 订阅事件
        _eventAggregator.Subscribe<DataUpdatedEvent>(HandleDataUpdated);
    }

    private void HandleDataUpdated(DataUpdatedEvent evt)
    {
        _logger.LogInformation("收到来自 {Source} 在 {Timestamp} 的数据更新", 
            evt.Source, evt.Timestamp);
        
        // 对事件做出反应
    }
    
    // 当插件卸载时不要忘记取消订阅
    public void Dispose()
    {
        _eventAggregator.Unsubscribe<DataUpdatedEvent>(HandleDataUpdated);
    }
}

// 事件类
public class DataUpdatedEvent
{
    public string Source { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}
```

## 响应式插件设计模式

### 响应式 UI 更新

```csharp
// 实时数据监控视图模型
public class MonitorViewModel : ReactiveObject
{
    private readonly IDataMonitorService _dataService;
    private readonly ObservableAsPropertyHelper<double> _currentValue;
    private readonly ObservableAsPropertyHelper<List<DataPoint>> _dataPoints;
    private readonly ObservableAsPropertyHelper<bool> _isConnected;

    public MonitorViewModel(IDataMonitorService dataService)
    {
        _dataService = dataService;
        
        // 将服务的实时数据转换为视图属性
        _currentValue = _dataService.CurrentValueStream
            .ToProperty(this, x => x.CurrentValue);
            
        _dataPoints = _dataService.DataStream
            .Scan(new List<DataPoint>(), (list, newPoint) => {
                var newList = new List<DataPoint>(list) { newPoint };
                // 只保留最后 100 个点
                return newList.Skip(Math.Max(0, newList.Count - 100)).ToList();
            })
            .ToProperty(this, x => x.DataPoints);
            
        _isConnected = _dataService.ConnectionStatusStream
            .ToProperty(this, x => x.IsConnected);
            
        // 命令
        ConnectCommand = ReactiveCommand.CreateFromTask(
            async () => await _dataService.ConnectAsync());
            
        DisconnectCommand = ReactiveCommand.Create(
            () => _dataService.Disconnect());
    }
    
    // 由实时流支持的只读属性
    public double CurrentValue => _currentValue.Value;
    public List<DataPoint> DataPoints => _dataPoints.Value;
    public bool IsConnected => _isConnected.Value;
    
    public ICommand ConnectCommand { get; }
    public ICommand DisconnectCommand { get; }
    
    // 清理资源
    public void Dispose()
    {
        _currentValue?.Dispose();
        _dataPoints?.Dispose();
        _isConnected?.Dispose();
    }
}
```

## 图表

### 插件生命周期图

```
┌─────────────────────────────────────────────────────────────────────────┐
│                         插件生命周期                                     │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  ┌─────────┐      ┌────────────┐      ┌────────────┐     ┌───────────┐  │
│  │  发现   │ ──▶ │   验证     │ ──▶ │   注册     │ ──▶ │   激活    │  │
│  └─────────┘      └────────────┘      └────────────┘     └───────────┘  │
│       │                 ▲                   │                 │         │
│       │                 │                   │                 │         │
│       ▼                 │                   ▼                 ▼         │
│  ┌──────────────┐       │           ┌─────────────┐    ┌─────────────┐  │
│  │ 插件文件     │       │           │ DI 服务     │    │   UI        │  │
│  │ - DLL        │       │           │ 注册        │    │   集成      │  │
│  │ - 配置       │       │           └─────────────┘    └─────────────┘  │
│  │ - 资源       │       │                                    │         │
│  └──────────────┘       │                                    │         │
│                         │                                    │         │
│  ┌─────────────┐   ┌────────────┐    ┌───────────────┐       │         │
│  │   处置     │ ◀─┤   卸载     │ ◀──┤   停用       │ ◀─────┘         │
│  └─────────────┘   └────────────┘    └───────────────┘                 │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

### 插件通信模式图

```
┌───────────────────────────────────────────────────────────────────────────┐
│                         插件通信模式                                        │
├───────────────────────────────────────────────────────────────────────────┤
│                                                                           │
│  ┌─────────────────┐                            ┌─────────────────┐       │
│  │    插件 A       │                            │    插件 B       │       │
│  ├─────────────────┤                            ├─────────────────┤       │
│  │                 │                            │                 │       │
│  │ ┌─────────────┐ │        模式 1              │ ┌─────────────┐ │       │
│  │ │  服务提供者  │ │       直接依赖注入         │ │  服务消费者  │ │       │
│  │ │             │ │ ────────────────────────▶ │ │             │ │       │
│  │ └─────────────┘ │                            │ └─────────────┘ │       │
│  │                 │                            │                 │       │
│  │ ┌─────────────┐ │        模式 2              │ ┌─────────────┐ │       │
│  │ │   事件      │ │ ◀────────────────────────▶ │ │   事件      │ │       │
│  │ │  发布者     │ │       事件聚合器           │ │  订阅者     │ │       │
│  │ └─────────────┘ │                            │ └─────────────┘ │       │
│  │                 │                            │                 │       │
│  └─────────────────┘                            └─────────────────┘       │
│          ▲                                               ▲                │
│          │                                               │                │
│          │                                               │                │
│          │                模式 3                         │                │
│          └───────────────────────────────────────────────┘                │
│                         共享服务                                           │
│                                                                           │
│  ┌─────────────────────────────────────────────────────────────────┐      │
│  │                      主机应用程序                                │      │
│  │ ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐   │      │
│  │ │ 事件聚合器      │  │ 服务注册表      │  │ 扩展点         │   │      │
│  │ └─────────────────┘  └─────────────────┘  └─────────────────┘   │      │
│  └─────────────────────────────────────────────────────────────────┘      │
│                                                                           │
└───────────────────────────────────────────────────────────────────────────┘
```

## 参考和进一步阅读

- [插件开发指南](plugin-development-guide.md) - 插件开发主要文档
- [插件 API 参考](plugin-api-reference.md) - 详细的 API 文档
- [ReactiveUI 文档](https://www.reactiveui.net/docs/) - 响应式 UI 模式
- [Avalonia UI 文档](https://docs.avaloniaui.net/) - UI 开发
