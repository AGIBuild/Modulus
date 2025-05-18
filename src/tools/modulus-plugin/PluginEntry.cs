using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Modulus.Plugin.Abstractions;

namespace Modulus.Plugin
{
    /// <summary>
    /// 插件开发模板示例入口类。
    /// 实现 IPlugin 接口，提供插件的核心功能。
    /// </summary>
    public class PluginEntry : IPlugin
    {
        private readonly PluginMeta _metadata = new();

        /// <summary>
        /// 获取插件元数据。
        /// </summary>
        /// <returns>插件元数据。</returns>
        public IPluginMeta GetMetadata() => _metadata;

        /// <summary>
        /// 注册插件服务到 DI 容器。
        /// </summary>
        /// <param name="services">服务集合。</param>
        /// <param name="configuration">插件配置。</param>
        public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            // TODO: 注册插件需要的服务
            // 例如: services.AddSingleton<IMyService, MyService>();
        }
        
        /// <summary>
        /// DI 容器构建完成后调用，用于初始化插件。
        /// </summary>
        /// <param name="provider">服务提供者，用于解析依赖。</param>
        public void Initialize(IServiceProvider provider)
        {
            // TODO: 解析服务并进行初始化
            // 例如: 
            // var logger = provider.GetService<ILogger<PluginEntry>>();
            // logger?.LogInformation("插件初始化完成");
        }
        
        /// <summary>
        /// 返回插件的主视图/控件（可选）。
        /// </summary>
        /// <returns>插件主视图。</returns>
        public object? GetMainView()
        {
            // TODO: 返回插件的主视图
            // 例如: return new Views.MainView();
            return null;
        }
          
        /// <summary>
        /// 返回插件的菜单或菜单扩展（可选）。
        /// </summary>
        /// <returns>插件菜单。</returns>
        public object? GetMenu()
        {
            // TODO: 返回插件的菜单
            // 例如: return new Views.PluginMenu();
            return null;
        }
    }

    /// <summary>
    /// 插件元数据实现类。
    /// </summary>
    public class PluginMeta : IPluginMeta
    {
        public string Name => "模板插件";
        public string Version => "1.0.0";
        public string Description => "Modulus插件模板";
        public string Author => "开发者";
        public string[]? Dependencies => null;
        public string ContractVersion => "2.0.0";
        public string? NavigationIcon => "\uE8A5"; // Segoe MDL2 Assets 图标
        public string? NavigationSection => "body";
        public int NavigationOrder => 100;
    }
}
