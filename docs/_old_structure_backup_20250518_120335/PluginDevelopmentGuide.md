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
9. [最佳实践](#最佳实践)

## 插件契约

Modulus 插件系统基于明确定义的契约接口，这些接口位于 `Modulus.Plugin.Abstractions` 程序集中。所有插件必须实现这些接口才能被主程序正确加载和使用。

### 核心接口

- **IPlugin**: 插件的主接口，包含元数据获取、服务注册、初始化和UI扩展点
- **IPluginMeta**: 插件元数据接口，包含名称、版本、描述、作者和依赖关系
- **ILocalizer**: 本地化接口，支持多语言资源访问与切换
- **IPluginSettings**: 插件配置接口，提供对插件特定配置的访问

## 目录结构

每个插件应当有以下标准化的目录结构：

```
MyPlugin/
  ├── MyPlugin.dll          # 主程序集
  ├── pluginsettings.json   # 插件配置
  ├── lang.en.json          # 英文语言资源
  ├── lang.zh.json          # 中文语言资源
  └── [其他依赖DLL]          # 插件依赖的其他程序集
```

### 配置文件示例 (pluginsettings.json)

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

### 语言资源文件示例 (lang.en.json)

```json
{
  "Hello": "Hello",
  "Goodbye": "Goodbye",
  "Welcome": "Welcome to Modulus",
  "Settings": "Settings"
}
```

## 插件生命周期

1. **发现与加载**：主程序扫描插件目录，查找实现 `IPlugin` 接口的程序集
2. **版本兼容性检查**：检查插件契约版本与主程序兼容性
3. **服务注册**：调用 `ConfigureServices` 方法注册插件服务
4. **初始化**：调用 `Initialize` 方法进行插件初始化
5. **UI集成**：通过 `GetMainView` 和 `GetMenu` 方法集成插件UI
6. **卸载**：插件被卸载时清理资源

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

## 配置系统

插件可以通过注入的 `IConfiguration` 访问 `pluginsettings.json` 中的配置：

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

## 本地化支持

插件可以通过注入的 `ILocalizer` 访问本地化资源：

```csharp
public class MyView
{
    private readonly ILocalizer _localizer;
    
    public MyView(ILocalizer localizer)
    {
        _localizer = localizer;
        
        // 获取当前语言的本地化字符串
        var hello = _localizer["Hello"];
        
        // 获取支持的语言列表
        var languages = _localizer.SupportedLanguages;
        
        // 切换语言
        _localizer.SetLanguage("zh");
    }
}
```

## 日志记录

插件可以通过注入 `ILogger<T>` 进行日志记录：

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
        _logger.LogInformation("执行操作");
        
        try
        {
            // ...
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "操作失败");
        }
    }
}
```

## 最佳实践

1. **遵循依赖注入原则**：使用构造函数注入依赖，避免静态访问和服务定位器模式
2. **适当隔离**：将逻辑分层，将界面、业务逻辑和数据访问分离
3. **异常处理**：捕获和记录异常，不要让异常传播到主程序
4. **资源释放**：正确实现资源释放模式，在插件卸载时释放所有资源
5. **版本兼容性**：在 `ContractVersion` 中正确声明插件的契约版本
6. **命名空间隔离**：使用唯一的命名空间前缀，避免与其他插件或主程序冲突

## 示例插件

参考示例插件实现：

- SimplePlugin：基本插件示例
- ExamplePlugin：完整功能演示
- NavigationExamplePlugin：导航和UI扩展示例

## 排障指南

- **插件无法加载**：检查契约版本兼容性、目录结构、程序集引用
- **服务无法解析**：确保正确注册服务，检查依赖关系
- **本地化失效**：检查语言文件格式和路径
- **配置无效**：检查 pluginsettings.json 格式和路径
