# Modulus 插件开发指南

本文档提供了在 Modulus 平台上开发插件的详细指南，包括插件契约、目录结构、基础能力和最佳实践。

## 目录

1. [插件契约](#插件契约)
2. [目录结构](#目录结构)
3. [核心接口](#核心接口)
4. [插件生命周期](#插件生命周期)
5. [依赖注入与服务注册](#依赖注入与服务注册)
6. [配置系统](#配置系统)
7. [本地化支持](#本地化支持)
8. [日志记录](#日志记录)
9. [UI 集成](#ui-集成)
10. [开发与打包](#开发与打包)
11. [插件安全与隔离](#插件安全与隔离)
12. [高级功能](#高级功能)
13. [最佳实践](#最佳实践)
14. [故障排除指南](#故障排除指南)
15. [性能优化](#性能优化)
16. [Visual Studio Code 集成](#visual-studio-code-集成)
17. [高级示例](#高级示例)
16. [示例插件](#示例插件)
17. [结论](#结论)

## 插件契约

Modulus 插件系统基于明确定义的契约接口，这些接口位于 `Modulus.Plugin.Abstractions` 程序集中。所有插件必须实现这些接口才能被主程序正确加载和使用。

### 插件架构图

以下图表说明了 Modulus 插件系统的架构以及插件如何与主应用程序交互：

```
┌───────────────────────────────────────────────────────────────────┐
│                       Modulus 主应用程序                           │
├───────────────┬───────────────────────────┬─────────────────────┬─┘
│ 插件加载器     │ 插件管理控制台            │ 核心服务            │
└─────┬─────────┴───────────────────────────┴─────────────────────┘
      │
      │                ┌───────────────┐     ┌───────────────┐
      └────加载────────► 程序集加载    │     │ 程序集加载    │
                       │ 上下文 1      │     │ 上下文 2      │
                       ├───────────────┤     ├───────────────┤
                       │ 插件 1        │     │ 插件 2        │
        集成           ├───────────────┤     ├───────────────┤
      ◄───接口─────────┤ IPlugin       │     │ IPlugin       │
                       ├───────────────┤     ├───────────────┤
                       │ 插件 UI       │     │ 插件 UI       │
                       ├───────────────┤     ├───────────────┤
                       │ 插件服务      │     │ 插件服务      │
                       └─────┬─────────┘     └─────┬─────────┘
                             │                     │
                             │                     │
                       ┌─────▼─────────────────────▼─────┐
                       │      插件资源                    │
                       │  (配置文件，文件，本地数据)      │
                       └───────────────────────────────┬─┘
```

这种架构提供：
- **隔离**：每个插件在自己的 `AssemblyLoadContext` 中运行，防止冲突
- **标准化集成**：插件使用定义明确的接口与主机交互
- **服务访问**：插件可以使用主机服务并提供特定于插件的服务
- **UI 集成**：插件可以为应用程序贡献 UI 元素

### 核心接口

- **IPlugin**: 插件的主接口，包含元数据获取、服务注册、初始化和UI扩展点
- **IPluginMeta**: 插件元数据接口，包含名称、版本、描述、作者和依赖关系
- **ILocalizer**: 本地化接口，支持多语言资源访问与切换
- **IPluginSettings**: 插件配置接口，提供对插件特定配置的访问

### 接口详细说明

#### IPlugin 接口

```csharp
public interface IPlugin
{
    /// <summary>
    /// 获取插件元数据（名称、版本、作者等）
    /// </summary>
    IPluginMeta GetMetadata();

    /// <summary>
    /// 注册插件服务到DI容器
    /// </summary>
    void ConfigureServices(IServiceCollection services, IConfiguration configuration);
    
    /// <summary>
    /// DI容器构建后调用，用于解析服务和执行初始化
    /// </summary>
    void Initialize(IServiceProvider provider);
    
    /// <summary>
    /// 返回插件主视图/控件（可选）
    /// </summary>
    object? GetMainView();
    
    /// <summary>
    /// 返回插件菜单或菜单扩展（可选）
    /// </summary>
    object? GetMenu();
}
```

#### IPluginMeta 接口

```csharp
public interface IPluginMeta
{
    /// <summary>
    /// 插件名称
    /// </summary>
    string Name { get; }
    
    /// <summary>
    /// 插件版本
    /// </summary>
    string Version { get; }
    
    /// <summary>
    /// 插件描述
    /// </summary>
    string Description { get; }
    
    /// <summary>
    /// 插件作者
    /// </summary>
    string Author { get; }
    
    /// <summary>
    /// 插件依赖列表（可选）
    /// </summary>
    string[]? Dependencies { get; }
    
    /// <summary>
    /// 插件构建的契约版本
    /// </summary>
    string ContractVersion { get; }
    
    /// <summary>
    /// 导航菜单图标字符（可选）
    /// 应为图标字体如Segoe MDL2 Assets中的字符
    /// </summary>
    string? NavigationIcon { get; }
    
    /// <summary>
    /// 插件在导航栏中的位置（可选）
    /// 可以是"header"、"body"或"footer"，默认为"body"
    /// </summary>
    string? NavigationSection { get; }
    
    /// <summary>
    /// 插件在导航部分中的顺序/位置（可选）
    /// 数字越小排序越靠前，默认为100
    /// </summary>
    int NavigationOrder { get; }
}
```

#### ILocalizer 接口

```csharp
public interface ILocalizer
{
    /// <summary>
    /// 通过键获取本地化字符串
    /// </summary>
    string this[string key] { get; }
    
    /// <summary>
    /// 当前语言代码（如"en"、"zh"）
    /// </summary>
    string CurrentLanguage { get; }
    
    /// <summary>
    /// 切换当前语言
    /// </summary>
    void SetLanguage(string lang);
    
    /// <summary>
    /// 支持的语言代码列表
    /// </summary>
    IEnumerable<string> SupportedLanguages { get; }
}
```

#### IPluginSettings 接口

```csharp
public interface IPluginSettings
{
    /// <summary>
    /// 插件特定配置
    /// </summary>
    IConfiguration Configuration { get; }
}

## 目录结构

每个插件应当有以下标准化的目录结构：
MyPlugin/
  ├── MyPlugin.dll          # 主程序集
  ├── pluginsettings.json   # 插件配置
  ├── lang.en.json          # 英文语言资源
  ├── lang.zh.json          # 中文语言资源
  └── [其他依赖DLL]          # 插件依赖的其他程序集
### 配置文件示例 (pluginsettings.json)
{
  "ContractVersion": "2.0.0",
  "Settings": {
    "MySetting1": "value1",
    "MySetting2": 123,
    "MySetting3": true
  }
}
### 语言资源文件示例 (lang.en.json)
{
  "Hello": "Hello",
  "Goodbye": "Goodbye",
  "Welcome": "Welcome to Modulus",
  "Settings": "Settings"
}
## 插件生命周期

1. **发现与加载**：主程序扫描插件目录，查找实现 `IPlugin` 接口的程序集
2. **版本兼容性检查**：检查插件契约版本与主程序兼容性
3. **服务注册**：调用 `ConfigureServices` 方法注册插件服务
4. **初始化**：调用 `Initialize` 方法进行插件初始化
5. **UI集成**：通过 `GetMainView` 和 `GetMenu` 方法集成插件UI
6. **卸载**：插件被卸载时清理资源

### 详细流程说明

1. **发现阶段**：
   - Modulus启动时会扫描用户插件目录（通常位于 `%USERPROFILE%\.modulus\plugins\` 或 `~/.modulus/plugins/`）
   - 系统通过检测目录中的DLL文件来发现潜在的插件
   - 同时会查找关联的`pluginsettings.json`配置文件

2. **加载与验证阶段**：
   - 使用专用的`AssemblyLoadContext`加载插件程序集，确保插件代码在隔离环境中运行
   - 系统会查找实现`IPlugin`接口的非抽象类作为插件入口点
   - 验证插件契约版本与主程序的兼容性

3. **服务注册与初始化阶段**：
   - 系统调用插件的`ConfigureServices`方法，允许插件注册自己的服务到依赖注入容器
   - 构建插件专用的服务提供者（ServiceProvider）
   - 调用插件的`Initialize`方法，允许插件进行初始化工作

4. **UI集成阶段**：
   - 主程序调用插件的`GetMainView`方法获取插件主界面
   - 调用插件的`GetMenu`方法获取插件菜单项（如果有）
   - 根据插件元数据中的导航信息（图标、位置等）将插件整合到主界面

5. **热重载监控**：
   - 系统会监控插件目录的变化（文件创建、修改、删除等）
   - 当检测到变化时，触发插件重新加载流程
   
6. **卸载阶段**：
   - 当插件需要卸载时（如应用关闭或插件更新），系统会尝试释放插件资源
   - 卸载插件的`AssemblyLoadContext`，回收内存

## 依赖注入与服务注册

插件可以通过 `ConfigureServices` 方法注册自己的服务：

```csharp
public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
{
    // 注册插件服务
    services.AddSingleton<IMyService, MyService>();
    services.AddScoped<IMyDataAccess, MyDataAccess>();
    
    // 可以使用配置
    var myOption = configuration.GetSection("Settings:MySetting1").Value;
    services.Configure<MyOptions>(configuration.GetSection("Settings"));
}
```

### 服务生命周期

在注册服务时，可以选择不同的生命周期：

- **Singleton**：创建单个实例，在整个应用程序生命周期内共享
  ```csharp
  services.AddSingleton<IMyService, MyService>();
  ```

- **Scoped**：为每个作用域创建一个实例（通常是每个请求一个实例）
  ```csharp
  services.AddScoped<IMyDataAccess, MyDataAccess>();
  ```

- **Transient**：每次请求时创建新实例
  ```csharp
  services.AddTransient<IMyProcessor, MyProcessor>();
  ```

### 获取宿主服务

插件可以通过 `Initialize` 方法从宿主应用程序获取服务：

```csharp
public void Initialize(IServiceProvider provider)
{
    // 获取日志记录器
    var logger = provider.GetService<ILogger<MyPluginEntry>>();
    
    // 获取插件的本地化器
    var localizer = provider.GetService<ILocalizer>();
    
    // 获取宿主应用程序的主窗口（如果可用）
    var mainWindow = provider.GetService<IMainWindow>();
}
```

### 服务注册最佳实践

1. **使用扩展方法**：将服务注册放在扩展方法中，使代码更清晰

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

2. **避免服务冲突**：使用唯一名称，避免与主应用程序或其他插件的服务冲突

3. **使用委托注册**：对于需要配置的服务，使用委托注册

   ```csharp
   services.AddSingleton<IMyConfigurableService>(sp => 
   {
       var config = sp.GetRequiredService<IConfiguration>();
       var logger = sp.GetRequiredService<ILogger<MyService>>();
       return new MyConfigurableService(config["Settings:Key"], logger);
   });
   ```

## 插件安全与隔离

Modulus 提供了强大的插件安全与隔离机制，以确保插件不会对主应用程序或其他插件产生不良影响。

### 加载上下文隔离

每个插件在其自己的 `AssemblyLoadContext` 中加载，提供强大的隔离并防止版本冲突：

```csharp
// 主应用程序插件加载器示例
public class PluginLoader
{
    public IPlugin LoadPlugin(string pluginPath)
    {
        // 为插件创建隔离的加载上下文
        var loadContext = new PluginLoadContext(pluginPath);
        
        // 加载插件程序集
        var assemblyPath = Path.Combine(pluginPath, "MyPlugin.dll");
        var assembly = loadContext.LoadFromAssemblyPath(assemblyPath);
        
        // 查找并实例化插件入口点
        var pluginType = assembly.GetTypes()
            .FirstOrDefault(t => typeof(IPlugin).IsAssignableFrom(t) && !t.IsAbstract);
            
        if (pluginType == null)
            throw new InvalidOperationException("没有找到插件入口点");
            
        return (IPlugin)Activator.CreateInstance(pluginType);
    }
}
```

### 权限管理

插件应根据其功能请求特定权限：

```csharp
public class MyPluginMeta : IPluginMeta
{
    public string Id => "MyPlugin";
    public string Name => "我的插件";
    public string Description => "这是一个示例插件";
    public Version Version => new Version(1, 0, 0);
    
    // 声明插件所需权限
    public string[] Permissions => new[]
    {
        PluginPermissions.FileSystem.Read,
        PluginPermissions.Network.Connect
    };
}
```

### 资源限制

防止恶意插件消耗过多资源：

```csharp
// 为插件操作设置资源限制
public void Initialize(IServiceProvider provider)
{
    // 获取资源限制服务
    var resourceManager = provider.GetRequiredService<IPluginResourceManager>();
    
    // 设置资源限制
    resourceManager.SetMemoryLimit(256 * 1024 * 1024); // 256 MB 内存上限
    resourceManager.SetCpuLimit(0.5); // 最多使用 50% CPU
    
    // 为网络操作设置限速
    resourceManager.SetNetworkRateLimit(1024 * 1024); // 1 MB/s
}
```

### 插件签名与验证

强烈建议为生产环境中的插件实施签名和验证：

```csharp
// 签名插件（在打包过程中）
public static void SignPlugin(string pluginPath, X509Certificate2 certificate)
{
    // 读取插件清单
    var manifestPath = Path.Combine(pluginPath, "plugin.manifest");
    var manifest = File.ReadAllText(manifestPath);
    
    // 计算内容散列
    using var sha256 = SHA256.Create();
    var contentBytes = Encoding.UTF8.GetBytes(manifest);
    var hash = sha256.ComputeHash(contentBytes);
    
    // 使用证书签名散列
    var signatureBytes = certificate.GetRSAPrivateKey().SignHash(hash, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
    var signature = Convert.ToBase64String(signatureBytes);
    
    // 将签名添加到插件包中
    File.WriteAllText(Path.Combine(pluginPath, "signature.txt"), signature);
}
```

### 沙箱化执行

对于高风险操作，使用沙箱执行：

```csharp
public async Task ExecuteRiskyOperationAsync(string script)
{
    // 获取沙箱服务
    var sandbox = _serviceProvider.GetRequiredService<IPluginSandbox>();
    
    // 在沙箱中执行代码
    var result = await sandbox.ExecuteAsync(script, new SandboxOptions
    {
        AllowedAssemblies = new[] { "System", "System.Core", "System.Linq" },
        DisableDangerousApis = true,
        Timeout = TimeSpan.FromSeconds(5)
    });
    
    // 处理结果
    if (result.Success)
    {
        _logger.LogInformation("沙箱执行成功: {Result}", result.ReturnValue);
    }
    else
    {
        _logger.LogError("沙箱执行失败: {Error}", result.Error);
    }
}
```

### 安全最佳实践

1. **最小权限原则**：仅请求插件功能所需的最小权限
2. **输入验证**：验证所有来自外部的数据
3. **安全存储**：使用加密存储敏感信息
4. **依赖扫描**：定期检查插件依赖项以获取已知漏洞
5. **代码审查**：实施插件代码的审查流程

有关更高级的安全实践，请查看[高级安全实践](#高级安全实践)章节。
## 高级功能

本节介绍 Modulus 插件系统的高级功能，适合有经验的开发者使用。

### 插件间通信

插件可以相互通信，共享功能和数据：

#### 事件聚合器模式

使用事件聚合器在插件之间进行松散耦合的通信：

```csharp
// 在服务注册中添加事件聚合器
public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
{
    services.AddSingleton<IEventAggregator, EventAggregator>();
}

// 发布事件
public class PublisherService
{
    private readonly IEventAggregator _eventAggregator;
    
    public PublisherService(IEventAggregator eventAggregator)
    {
        _eventAggregator = eventAggregator;
    }
    
    public void UpdateData(string data)
    {
        // 处理数据...
        
        // 发布事件通知其他插件
        _eventAggregator.Publish(new DataUpdatedEvent { Data = data });
    }
}

// 订阅事件
public class SubscriberService : IDisposable
{
    private readonly IEventAggregator _eventAggregator;
    
    public SubscriberService(IEventAggregator eventAggregator)
    {
        _eventAggregator = eventAggregator;
        
        // 订阅事件
        _eventAggregator.Subscribe<DataUpdatedEvent>(OnDataUpdated);
    }
    
    private void OnDataUpdated(DataUpdatedEvent evt)
    {
        // 处理更新的数据
        Console.WriteLine($"接收到更新的数据: {evt.Data}");
    }
    
    public void Dispose()
    {
        // 取消订阅以防止内存泄漏
        _eventAggregator.Unsubscribe<DataUpdatedEvent>(OnDataUpdated);
    }
}

// 事件数据类
public class DataUpdatedEvent
{
    public string Data { get; set; }
}
```

查看[高级示例文档](plugin-advanced-examples.md)中的完整事件聚合器实现。

#### 服务依赖注入

一个插件可以将服务注册到 DI 容器中，另一个插件可以使用这些服务：

```csharp
// 插件 A: 服务提供者
public class ProviderPlugin : IPlugin
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // 注册可供其他插件使用的服务
        services.AddSingleton<IDataService, DataService>();
    }
}

// 插件 B: 服务消费者
public class ConsumerPlugin : IPlugin
{
    // 声明依赖关系
    public class ConsumerMeta : IPluginMeta
    {
        // 其他元数据...
        
        public string[] Dependencies => new[] { "ProviderPlugin" };
    }
    
    // 使用提供者插件中的服务
    public void Initialize(IServiceProvider serviceProvider)
    {
        var dataService = serviceProvider.GetRequiredService<IDataService>();
        // 使用服务...
    }
}
```

### 插件热重载

支持在不重启应用程序的情况下更新插件：

```csharp
public class HotReloadablePlugin : IPlugin, IDisposable
{
    private FileSystemWatcher _watcher;
    private IServiceProvider _serviceProvider;
    
    public void Initialize(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        
        // 设置文件监视器以检测更改
        _watcher = new FileSystemWatcher(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
        _watcher.Filter = "*.dll";
        _watcher.NotifyFilter = NotifyFilters.LastWrite;
        _watcher.Changed += OnAssemblyChanged;
        _watcher.EnableRaisingEvents = true;
    }
    
    private void OnAssemblyChanged(object sender, FileSystemEventArgs e)
    {
        // 获取插件热重载服务
        var reloader = _serviceProvider.GetRequiredService<IPluginReloader>();
        
        // 请求重新加载此插件
        reloader.RequestReload(GetMetadata().Id);
    }
    
    public void Dispose()
    {
        _watcher?.Dispose();
    }
}
```

### 高级本地化

通过本地化管理器实现动态切换和复杂多语言支持：

```csharp
public class AdvancedLocalization
{
    private readonly ILocalizationManager _localizationManager;
    
    public AdvancedLocalization(ILocalizationManager localizationManager)
    {
        _localizationManager = localizationManager;
        
        // 监听语言变化
        _localizationManager.LanguageChanged += OnLanguageChanged;
    }
    
    private void OnLanguageChanged(object sender, LanguageChangedEventArgs e)
    {
        Console.WriteLine($"语言已更改为: {e.NewLanguage}");
        
        // 重新加载本地化资源
        UpdateUI();
    }
    
    public string GetLocalizedText(string key, params object[] parameters)
    {
        // 支持带参数的格式化字符串
        return _localizationManager.GetString(key, parameters);
    }
    
    public void SwitchLanguage(string languageCode)
    {
        // 动态切换语言
        _localizationManager.SetCurrentLanguage(languageCode);
    }
}
```

### 插件高级配置

实现动态、分层和加密的配置设置：

```csharp
public class AdvancedConfiguration
{
    private readonly IPluginConfiguration _configuration;
    private readonly IEncryptionService _encryptionService;
    
    public AdvancedConfiguration(
        IPluginConfiguration configuration,
        IEncryptionService encryptionService)
    {
        _configuration = configuration;
        _encryptionService = encryptionService;
    }
    
    public void SaveEncryptedSetting(string key, string sensitiveValue)
    {
        // 加密敏感配置值
        var encryptedValue = _encryptionService.Encrypt(sensitiveValue);
        
        // 保存加密值
        _configuration.Set($"Secure:{key}", encryptedValue);
        await _configuration.SaveChangesAsync();
    }
    
    public string GetEncryptedSetting(string key)
    {
        // 获取加密值
        var encryptedValue = _configuration.GetValue<string>($"Secure:{key}");
        
        if (string.IsNullOrEmpty(encryptedValue))
            return null;
            
        // 解密值
        return _encryptionService.Decrypt(encryptedValue);
    }
    
    public void MonitorConfigurationChanges()
    {
        // 订阅配置变更通知
        _configuration.OnChange(HandleConfigurationChanged);
    }
    
    private void HandleConfigurationChanged(ConfigChangedEventArgs args)
    {
        Console.WriteLine($"配置已更改: {args.Key} = {args.NewValue}");
        
        // 通知组件重新加载配置
    }
}
```

想要更多高级示例和详细指南？请查看[插件高级示例](plugin-advanced-examples.md)文档，其中包含更多的代码示例和深入讲解。
## 最佳实践

### 设计与架构

1. **遵循依赖注入原则**
   - 使用构造函数注入依赖，避免静态访问和服务定位器模式
   - 优先使用接口而非具体实现类型
   - 使用适当的服务注册生命周期（Singleton、Scoped、Transient）

   ```csharp
   // 推荐
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

   // 避免
   public class MyService
   {
       public void DoSomething()
       {
           var logger = ServiceLocator.GetService<ILogger>();
           var data = StaticDataAccess.GetData();
       }
   }
   ```

2. **适当的隔离**
   - 分层设计逻辑，分离 UI、业务逻辑和数据访问
   - 对 UI 代码使用 MVVM 或类似的架构模式
   - 创建清晰的 API 边界

   ```
   MyPlugin/
   ├── Models/           # 数据模型
   ├── ViewModels/       # 视图模型
   ├── Views/            # UI 视图
   ├── Services/         # 业务逻辑服务
   └── Data/             # 数据访问层
   ```

3. **兼容性考虑**
   - 在 `ContractVersion` 中正确声明插件的契约版本
   - 使用条件编译（`#if` 指令）处理不同版本之间的 API 差异
   - 避免使用 Modulus 内部 API，只使用已发布的接口

4. **命名空间隔离**
   - 使用唯一的命名空间前缀，避免与其他插件或主程序冲突
   - 例如：`CompanyName.ProductName.PluginName`

### 编码实践

1. **异常处理**
   - 捕获并记录异常，不要让它们传播到主程序
   - 提供有意义的错误消息
   - 在 UI 层显示用户友好的错误消息

   ```csharp
   public void ProcessData()
   {
       try
       {
           // 业务逻辑
       }
       catch (SpecificException ex)
       {
           _logger.LogError(ex, "特定错误处理");
           // 处理已知异常
       }
       catch (Exception ex)
       {
           _logger.LogError(ex, "处理数据时出错");
           // 通用错误处理
       }
   }
   ```

2. **资源处理**
   - 正确实现资源处理模式，在插件卸载时释放所有资源
   - 实现 `IDisposable` 接口（如需要）
   - 取消长时间运行的任务
   - 关闭打开的连接和文件句柄

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
                   // 释放其他托管资源
               }

               // 清理非托管资源

               _disposed = true;
           }
       }
   }
   ```

3. **配置管理**
   - 使用强类型配置（通过 `IOptions<T>`）而不是直接访问字符串键值
   - 提供合理的默认值
   - 验证配置值

   ```csharp
   // 注册配置
   services.Configure<MyPluginOptions>(configuration.GetSection("Settings"));

   // 使用配置
   public class MyService
   {
       private readonly MyPluginOptions _options;

       public MyService(IOptions<MyPluginOptions> options)
       {
           _options = options.Value;
       }
   }

   // 配置类
   public class MyPluginOptions
   {
       public string Setting1 { get; set; } = "默认值";
       public int Setting2 { get; set; } = 42;

       public bool IsValid()
       {
           return !string.IsNullOrEmpty(Setting1) && Setting2 > 0;
       }
   }
   ```

### 测试与质量保证

1. **编写单元测试**
   - 为核心业务逻辑编写单元测试
   - 使用依赖注入简化测试
   - 使用模拟框架（如 Moq）隔离依赖

   ```csharp
   public void ProcessData_ValidInput_ReturnsExpectedResult()
   {
       // 安排
       var loggerMock = new Mock<ILogger<MyService>>();
       var dataServiceMock = new Mock<IDataService>>();
       dataServiceMock.Setup(x => x.GetData()).Returns(testData);
       
       var service = new MyService(loggerMock.Object, dataServiceMock.Object);
       
       // 执行
       var result = service.ProcessData();
       
       // 断言
       Assert.Equal(expectedResult, result);
   }
   ```

2. **集成测试**
   - 在 Modulus 环境中测试插件行为
   - 验证插件加载、初始化和卸载
   - 测试与其他组件的集成

3. **日志记录**
   - 在关键点使用适当的日志级别（Information、Warning、Error）
   - 包含上下文信息，但避免记录敏感信息
   - 使用结构化日志记录

### 部署与分发

1. **使用统一构建系统**
   - 尽可能使用Nuke构建系统来开发和打包插件，确保一致性
   - 遵循版本控制和发布流程
   - 维护清晰的变更日志

2. **测试在示例插件上**
   - 开发新功能时，先在示例插件上测试，再集成到自己的插件中
   - 利用示例插件作为学习和调试工具

3. **文档和支持**
   - 提供详细的安装和使用说明
   - 记录配置选项和API使用
   - 提供技术支持渠道

## 逐步教程

本部分提供特定插件开发场景的详细步骤指南。

### 教程 1：创建具有数据库访问的插件

本教程将引导您创建一个能够访问数据库并在 UI 中显示数据的插件。

#### 步骤 1：创建插件项目

首先，使用模板创建一个新的插件项目：

```powershell
dotnet new modulus-plugin -n DatabasePlugin
```

#### 步骤 2：添加数据库依赖项

添加必要的 Entity Framework Core 包：

```powershell
cd DatabasePlugin
dotnet add package Microsoft.EntityFrameworkCore.Sqlite
dotnet add package Microsoft.EntityFrameworkCore.Design
```

#### 步骤 3：创建数据模型和 DbContext

创建数据模型和数据库上下文，包括联系人基本信息字段和数据访问方法。

#### 步骤 4：创建数据访问服务

实现数据访问接口和服务，包括 CRUD 操作方法。

#### 步骤 5：创建视图模型

创建响应式视图模型，使用 ReactiveUI 实现数据绑定和命令。

#### 步骤 6：创建视图

设计 Avalonia UI 视图，使用数据网格和按钮展示联系人数据。

#### 步骤 7：在扩展方法中注册服务

创建扩展方法，注册数据库上下文和服务到依赖注入容器。

#### 步骤 8：更新插件入口点

实现 IPlugin 接口，初始化数据库并提供主视图。

#### 步骤 9：创建配置文件

配置数据库路径和其他插件设置。

#### 步骤 10：打包并部署插件

使用 Nuke 构建和打包插件：

```powershell
nuke plugin --op single --name DatabasePlugin
```

### 教程 2：创建具有外部 API 集成的插件

本教程演示如何创建一个与外部 API 集成的插件。主要步骤包括：

1. 创建插件项目结构
2. 添加 HTTP 客户端依赖项
3. 设计和实现 API 响应模型
4. 创建 API 服务和数据转换
5. 构建用户界面展示 API 数据

### 教程 3：创建具有实时更新的插件

本教程展示如何创建一个使用后台服务提供实时更新的插件。主要步骤包括：

1. 设置后台服务框架
2. 实现实时数据收集
3. 创建响应式 UI 更新机制
4. 管理插件生命周期和资源释放

## 故障排除指南

### 常见问题与解决方案

1. **插件无法加载**

   **可能原因**：
   - 契约版本不兼容
   - 缺少依赖项
   - 程序集冲突

   **解决方案**：
   - 检查插件的 `ContractVersion` 是否与 Modulus 兼容
   - 确保插件目录中包含所有依赖项
   - 检查程序集冲突，尝试使用不同的命名空间

2. **服务无法解析**

   **可能原因**：
   - 服务未正确注册
   - 依赖关系问题
   - 作用域问题

   **解决方案**：
   - 确保在 `ConfigureServices` 方法中正确注册服务
   - 检查依赖注入容器配置
   - 验证服务生命周期（Singleton、Scoped、Transient）

3. **本地化不起作用**

   **可能原因**：
   - 语言文件格式错误
   - 语言文件路径问题
   - 资源键不存在

   **解决方案**：
   - 确保语言文件使用正确的 JSON 格式
   - 检查语言文件是否正确命名（lang.en.json、lang.zh.json 等）
   - 验证所有语言文件包含相同的资源键

4. **配置无效**

   **可能原因**：
   - pluginsettings.json 格式错误
   - 配置路径问题
   - 缺少默认值

   **解决方案**：
   - 验证 JSON 格式是否正确
   - 检查配置路径是否与代码中使用的匹配
   - 为关键配置提供合理的默认值

5. **UI 不显示**

   **可能原因**：
   - `GetMainView` 方法未正确实现
   - 视图创建失败
   - 导航元数据问题

   **解决方案**：
   - 确保 `GetMainView` 返回有效的 UI 控件
   - 检查视图创建过程中的异常
   - 验证 `NavigationIcon` 和 `NavigationSection` 设置

6. **打包错误**

   **可能原因**：
   - 项目文件结构问题
   - 缺少必要文件
   - 构建配置错误

   **解决方案**：
   - 检查项目文件结构
   - 确保正确包含所有必要文件
   - 验证构建配置和依赖项

### 调试技巧

1. **启用详细日志记录**

   设置日志级别为 Debug 或 Trace：
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

2. **在调试模式下运行**

   在调试模式下启动 Modulus，并在插件代码中设置断点。

3. **检查插件加载输出**

   Modulus 在启动时输出插件加载信息，检查这些日志以诊断问题。

4. **使用插件沙箱模式**

   如果可能，使用 Modulus 的插件沙箱模式进行测试，这可以防止不稳定的插件崩溃整个应用程序。

5. **检查插件隔离问题**

   使用反射和加载上下文调试工具检查程序集加载或类型加载问题。

## 性能优化

优化插件性能对于提供流畅的用户体验至关重要：

### 加载性能优化

1. **最小化依赖**：
   - 仅包含必要的依赖项
   - 除非真正需要，否则避免使用大型框架
   - 考虑懒加载技术

2. **资源优化**：
   - 压缩图像和其他资源
   - 使用适当的图像格式（例如，使用 SVG 而不是高分辨率 PNG）
   - 延迟加载不立即需要的资源

3. **初始化优化**：
   - 推迟耗时的初始化直到必要时才执行
   - 使用异步初始化模式
   - 使用优先级队列优先初始化最重要的组件

   ```csharp
   public async void Initialize(IServiceProvider provider)
   {
       // 立即初始化关键组件
       InitializeCore();
       
       // 异步初始化非关键组件
       await Task.Run(() => InitializeNonCritical());
   }
   ```

### 运行时性能优化

1. **UI 性能**：
   - 实现虚拟化技术（对于长列表）
   - 避免复杂的绑定和计算属性
   - 使用适当的缓存策略

2. **内存管理**：
   - 避免内存泄漏
   - 适当释放不再需要的资源
   - 为频繁创建/销毁的对象实现对象池模式

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

3. **异步编程**：
   - 对耗时操作使用异步模式
   - 避免阻塞 UI 线程
   - 考虑使用响应式扩展（Reactive Extensions）处理事件流

### 诊断与监控

1. **性能分析**：
   - 使用分析工具识别瓶颈
   - 监控启动时间和资源使用情况
   - 为关键操作实现性能计时

   ```csharp
   public void MeasureOperation()
   {
       var sw = Stopwatch.StartNew();
       
       // 执行操作
       PerformOperation();
       
       sw.Stop();
       _logger.LogDebug("操作在 {ElapsedMs}ms 内完成", sw.ElapsedMilliseconds);
   }
   ```

2. **监控框架**：
   - 考虑实现插件性能监控框架
   - 跟踪插件资源使用情况
   - 向用户报告性能问题

## Visual Studio Code 集成

现代开发工作流程通常涉及 Visual Studio Code，Modulus 插件开发也不例外。本节介绍如何在 VS Code 中设置最佳的 Modulus 插件开发环境。

### 推荐扩展

为了获得最佳的开发体验，推荐以下 VS Code 扩展：

1. **C# 扩展 (ms-dotnettools.csharp)**：为 C# 提供 IntelliSense、调试和代码导航
2. **Avalonia for VS Code (avaloniateam.vscode-avalonia)**：提供 Avalonia UI 的 XAML 支持
3. **XML Tools (dotjoshjohnson.xml)**：增强的 XML 编辑体验
4. **.NET Core Test Explorer (formulahendry.dotnet-test-explorer)**：.NET 的可视化测试运行器

### 设置开发环境

1. **创建启动配置**

   在项目目录中创建 `.vscode/launch.json` 文件：

   ```json
   {
     "version": "0.2.0",
     "configurations": [
       {
         "name": "调试 Modulus 插件",
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

2. **添加任务配置**

   创建 `.vscode/tasks.json` 文件：

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

3. **配置 Intellisense**

   创建 `.vscode/settings.json` 文件：

   ```json
   {
     "omnisharp.enableRoslynAnalyzers": true,
     "omnisharp.enableEditorConfigSupport": true,
     "csharp.format.enable": true,
     "editor.formatOnSave": true
   }
   ```

### 使用热重载进行调试

VS Code 支持 .NET 应用程序的热重载，允许您在调试时修改插件：

1. **启用热重载**

   在插件项目文件中添加以下内容：

   ```xml
   <PropertyGroup>
     <EnableHotReload>true</EnableHotReload>
   </PropertyGroup>
   ```

2. **使用 XAML 热重载**

   在调试会话期间编辑 XAML 文件时，更改将立即反映在运行中的应用程序中。

3. **将热重载与 Modulus 的插件系统结合使用**

   由于 Modulus 有自己的插件热重载系统，您可以结合使用两者以获得最佳开发体验：

   ```csharp
   public class DevPlugin : IPlugin, IDisposable
   {
       private readonly CancellationTokenSource _cts = new();
       private readonly List<IDisposable> _subscriptions = new();
       
       // 插件初始化
       public void Initialize(IServiceProvider provider)
       {
           // 订阅事件，保存订阅以便后续取消
           var eventAggregator = provider.GetService<IEventAggregator>();
           var subscription = eventAggregator.Subscribe<MyEvent>(HandleEvent);
           _subscriptions.Add(subscription);
           
           // 启动后台任务
           RunBackgroundTask(_cts.Token);
       }
       
       // 资源清理
       public void Dispose()
       {
           _cts.Cancel();
           _cts.Dispose();
           
           foreach (var subscription in _subscriptions)
           {
               subscription.Dispose();
           }
           _subscriptions.Clear();
       }
   }
   ```

## 高级安全实践

安全性是开发扩展应用程序功能的插件时的关键考虑因素。本节介绍 Modulus 插件开发的高级安全实践。

### 插件签名

虽然默认情况下不是必需的，但插件签名提供了额外的安全性和真实性保证：

1. **创建签名证书**

   ```powershell
   # 生成用于开发的自签名证书
   $cert = New-SelfSignedCertificate -Subject "CN=ModulusPluginDev" -Type CodeSigning -CertStoreLocation Cert:\CurrentUser\My
   
   # 将证书导出到 PFX 文件
   $password = ConvertTo-SecureString -String "YourPassword" -Force -AsPlainText
   Export-PfxCertificate -Cert $cert -FilePath "ModulusPluginDev.pfx" -Password $password
   ```

2. **签名插件程序集**

   ```powershell
   # 为插件程序集签名
   Set-AuthenticodeSignature -FilePath "path\to\YourPlugin.dll" -Certificate $cert
   ```

3. **配置 Modulus 验证签名**

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

### 数据安全

在插件中处理敏感数据时：

1. **安全存储**

   使用 Modulus 提供的 `SecureDataStorage` 服务存储敏感信息：

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

2. **安全通信**

   与外部服务通信时，始终使用安全协议：

   ```csharp
   public async Task<string> FetchDataSecurelyAsync(string url)
   {
       using var client = new HttpClient();
       // 在生产环境中始终验证证书
       client.DefaultRequestHeaders.Add("User-Agent", "Modulus Plugin");
       
       var response = await client.GetAsync(url);
       response.EnsureSuccessStatusCode();
       
       return await response.Content.ReadAsStringAsync();
   }
   ```

3. **安全配置**

   切勿在明文配置文件中存储机密：

   ```csharp
   // 避免这样做：
   // {
   //   "ApiKey": "my-secret-api-key"
   // }
   
   // 相反，使用安全存储并在需要时请求凭据
   public async Task InitializeAsync()
   {
       if (!await _secureStorage.ExistsAsync("ApiKey"))
       {
           // 如果未存储 API 密钥，则提示用户
           var apiKey = await _dialogService.PromptForSecretAsync("输入 API 密钥");
           await _secureStorage.SaveAsync("ApiKey", apiKey);
       }
   }
   ```

### 代码安全

遵循以下实践确保插件代码安全：

1. **输入验证**

   始终验证输入，特别是来自外部源的输入：

   ```csharp
   public void ProcessUserInput(string input)
   {
       if (string.IsNullOrEmpty(input))
       {
           throw new ArgumentException("输入不能为空");
       }
       
       // 验证长度
       if (input.Length > 1000)
       {
           throw new ArgumentException("输入过长");
       }
       
       // 验证格式（例如，使用正则表达式）
       if (!Regex.IsMatch(input, @"^[a-zA-Z0-9\s]+$"))
       {
           throw new ArgumentException("输入包含无效字符");
       }
       
       // 处理验证过的输入
       // ...
   }
   ```

2. **安全反序列化**

   反序列化数据时要谨慎，特别是来自不受信任源的数据：

   ```csharp
   // 使用安全的反序列化选项
   var options = new JsonSerializerOptions
   {
       MaxDepth = 10, // 防止堆栈溢出攻击
       PropertyNameCaseInsensitive = true
   };
   
   try
   {
       var data = JsonSerializer.Deserialize<MyDataClass>(jsonString, options);
       // 处理数据
   }
   catch (JsonException ex)
   {
       _logger.LogError(ex, "无效的 JSON 数据");
       // 处理错误
   }
   ```

3. **最小权限原则**

   只请求您需要的权限，并清楚记录插件的功能：

   ```csharp
   [PluginPermission("FileSystem", "仅对插件目录的读取访问权限")]
   [PluginPermission("Network", "对 api.example.com 的访问权限")]
   public class MyPlugin : IPlugin
   {
       // 插件实现
   }
   ```

### 运行时安全监控

在插件中实现安全监控：

1. **记录安全事件**

   ```csharp
   public void ProcessUserAction(string userId, string action)
   {
       _logger.LogInformation("用户 {UserId} 执行了 {Action}", userId, action);
       
       if (IsSensitiveAction(action))
       {
           _logger.LogWarning("敏感操作 {Action} 由用户 {UserId} 执行", action, userId);
           // 可能还需要通知管理员或实施额外验证
       }
   }
   ```

2. **实现速率限制**

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
           
           // 如果周期已过，重置计数器
           if ((DateTime.UtcNow - state.LastReset).TotalMinutes >= periodMinutes)
           {
               state = (0, DateTime.UtcNow);
           }
           
           // 检查是否超出限制
           if (state.Count >= maxRequests)
           {
               return false; // 超出速率限制
           }
           
           // 更新计数器
           _requestCounts[clientId] = (state.Count + 1, state.LastReset);
           return true;
       }
   }
   ```

通过实施这些高级安全实践，您可以确保插件不仅功能良好，还能维护 Modulus 应用程序及其用户数据的安全性和完整性。

## 高级示例

为了帮助您更好地理解 Modulus 插件开发中的高级概念和模式，我们提供了一个专门的[高级示例文档](plugin-advanced-examples.md)，其中包含以下内容：

- **Avalonia ReactiveUI 集成**：使用响应式编程创建高响应性 UI
- **自定义插件设置 UI**：创建高级设置界面
- **插件通信模式**：使用依赖解析和事件聚合器进行插件间通信
- **响应式插件设计模式**：实现实时数据监控和响应式 UI 更新
- **详细图表**：展示插件生命周期和通信模式

这些示例提供了可工作的代码，您可以直接将其整合到您的插件项目中，帮助您更快地掌握高级插件开发技术。
## 示例插件

参考以下示例插件实现：

- **SimplePlugin**：演示最小插件实现的基本示例
- **ExamplePlugin**：包括配置、本地化、服务和 UI 的完整功能演示
- **NavigationExamplePlugin**：导航和 UI 扩展示例，展示菜单集成和导航自定义

这些示例插件可以在 `src/samples/` 目录中找到。

## 结论

开发 Modulus 插件是扩展应用程序功能的强大方式。本指南为开发高质量、高性能和可维护的插件提供了基础。

通过遵循最佳实践、良好组织的目录结构和清晰的代码设计，您可以创建与 Modulus 生态系统无缝集成的插件。

请记住：

- **关注用户体验**：创建直观、响应迅速的插件界面
- **保持可维护性**：编写清晰、结构良好的代码
- **尊重平台**：保持与 Modulus 设计原则和 UI 风格的一致性
- **持续改进**：收集用户反馈并迭代您的插件

我们期待看到您使用 Modulus 插件系统创建的创新解决方案！

---

**相关资源**：
- [Modulus API 文档](https://docs.modulus.org/api)
- [插件开发示例仓库](https://github.com/modulus/plugin-samples)
- [开发者社区论坛](https://community.modulus.org)
- [视频教程：从零开始开发 Modulus 插件](https://learn.modulus.org/plugins)
