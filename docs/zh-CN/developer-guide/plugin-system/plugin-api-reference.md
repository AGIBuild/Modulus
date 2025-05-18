# 插件 API 参考

本文档提供了 Modulus 插件 API 接口及其用法的详细参考。

## 核心接口

### IPlugin

所有插件必须实现的主要接口。

```csharp
public interface IPlugin
{
    /// <summary>
    /// 获取插件元数据。
    /// </summary>
    IPluginMeta Meta { get; }
    
    /// <summary>
    /// 为插件配置服务。
    /// </summary>
    /// <param name="services">用于注册服务的服务集合。</param>
    /// <param name="configuration">插件的配置。</param>
    void ConfigureServices(IServiceCollection services, IConfiguration configuration);
    
    /// <summary>
    /// 使用已解析的服务提供者初始化插件。
    /// </summary>
    /// <param name="provider">包含已注册服务的服务提供者。</param>
    void Initialize(IServiceProvider provider);
    
    /// <summary>
    /// 获取插件的主视图。如果插件不提供 UI，则返回 null。
    /// </summary>
    /// <returns>主视图对象，通常是 Avalonia 控件，或 null。</returns>
    object? GetMainView();
    
    /// <summary>
    /// 获取插件的菜单项。如果插件不提供菜单项，则返回 null。
    /// </summary>
    /// <returns>菜单项或 null。</returns>
    object? GetMenu();
}
```

### IPluginMeta

定义插件的元数据。

```csharp
public interface IPluginMeta
{
    /// <summary>
    /// 获取插件的显示名称。
    /// </summary>
    string Name { get; }
    
    /// <summary>
    /// 获取插件的版本。
    /// </summary>
    string Version { get; }
    
    /// <summary>
    /// 获取插件的描述。
    /// </summary>
    string Description { get; }
    
    /// <summary>
    /// 获取插件的作者。
    /// </summary>
    string Author { get; }
    
    /// <summary>
    /// 获取这个插件依赖的插件 ID 数组，如果没有依赖则为 null。
    /// </summary>
    string[]? Dependencies { get; }
    
    /// <summary>
    /// 获取此插件所需的插件契约版本。
    /// </summary>
    string ContractVersion { get; }
    
    /// <summary>
    /// 获取导航的可选图标，或 null。
    /// </summary>
    string? NavigationIcon { get; }
    
    /// <summary>
    /// 获取导航分组的可选部分，或 null。
    /// </summary>
    string? NavigationSection { get; }
    
    /// <summary>
    /// 获取此插件在导航中的顺序。
    /// </summary>
    int NavigationOrder { get; }
}
```

### ILocalizer

为插件提供本地化服务。

```csharp
public interface ILocalizer
{
    /// <summary>
    /// 获取指定键的本地化字符串。
    /// </summary>
    /// <param name="key">资源键。</param>
    /// <returns>本地化字符串，如果未找到则返回键本身。</returns>
    string this[string key] { get; }
    
    /// <summary>
    /// 获取当前语言代码。
    /// </summary>
    string CurrentLanguage { get; }
    
    /// <summary>
    /// 设置当前语言。
    /// </summary>
    /// <param name="language">要设置的语言代码。</param>
    void SetLanguage(string language);
    
    /// <summary>
    /// 获取支持的语言代码集合。
    /// </summary>
    IEnumerable<string> SupportedLanguages { get; }
}
```

## 扩展点

### UI 集成

插件可以通过以下方式与主应用程序 UI 集成：

1. **主视图**：实现 `GetMainView()` 以返回 UI 控件（通常是 Avalonia 控件），该控件将在选择插件时显示在主内容区域中。

2. **菜单扩展**：实现 `GetMenu()` 以返回将添加到应用程序菜单的菜单项。

示例：

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
            Header = "我的插件",
            Icon = "\uE8A5",
            Items = new List<MenuItemViewModel>
            {
                new MenuItemViewModel 
                { 
                    Header = "操作 1", 
                    Command = new RelayCommand(ExecuteAction1) 
                },
                new MenuItemViewModel 
                { 
                    Header = "操作 2", 
                    Command = new RelayCommand(ExecuteAction2) 
                }
            }
        }
    };
}
```

### 服务注册

插件可以在 `ConfigureServices` 阶段注册自己的服务：

```csharp
public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
{
    // 注册单例服务（插件生命周期内一个实例）
    services.AddSingleton<IMyService, MyService>();
    
    // 注册瞬态服务（每次都是新实例）
    services.AddTransient<IMyTransientService, MyTransientService>();
    
    // 使用配置
    services.Configure<MyOptions>(configuration.GetSection("MyOptions"));
}
```

## API 示例

### 基本插件实现

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
        _logger.LogInformation("插件 {PluginName} 已初始化", Meta.Name);
    }
    
    public object? GetMainView() => new MyPluginView();
    
    public object? GetMenu() => null; // 无菜单扩展
}

public class MyPluginMeta : IPluginMeta
{
    public string Name => "我的插件";
    public string Version => "1.0.0";
    public string Description => "Modulus 示例插件";
    public string Author => "开发者姓名";
    public string[]? Dependencies => null;
    public string ContractVersion => "2.0.0";
    public string? NavigationIcon => "\uE8A5";
    public string? NavigationSection => "工具";
    public int NavigationOrder => 100;
}
```

### 使用本地化

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

### 配置使用

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
