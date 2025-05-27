using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Modulus.Plugin.Abstractions;
using Modulus.App.Options;

namespace Modulus.App.Services
{
    /// <summary>
    /// 插件管理器实现
    /// </summary>
    public class PluginManager : IPluginManager
    {
        private readonly NavigationPluginService _navigationPluginService;
        private readonly IServiceCollection _services;
        private readonly IConfiguration _configuration;
        private readonly IOptionsMonitor<PluginOptions> _pluginOptions;
        private readonly IValidateOptions<PluginOptions> _pluginOptionsValidator;
        private IServiceProvider _serviceProvider; // Kept as field, ensure it's initialized before use.

        public List<IPlugin> LoadedPlugins { get; } = new();

        /// <summary>
        /// 创建插件管理器实例
        /// </summary>
        public PluginManager(
            NavigationPluginService navigationPluginService,
            IConfiguration configuration,
            IOptionsMonitor<PluginOptions> pluginOptions,
            IValidateOptions<PluginOptions> pluginOptionsValidator)
        {
            _navigationPluginService = navigationPluginService ?? throw new ArgumentNullException(nameof(navigationPluginService));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _pluginOptions = pluginOptions ?? throw new ArgumentNullException(nameof(pluginOptions));
            _pluginOptionsValidator = pluginOptionsValidator ?? throw new ArgumentNullException(nameof(pluginOptionsValidator));
            
            // 初始化服务集合
            _services = new ServiceCollection();
            _services.AddSingleton(_configuration);
            // Initial build of service provider, might be rebuilt if plugins add services.
            _serviceProvider = _services.BuildServiceProvider(); 
            
            // 验证和应用初始配置
            var validationResult = _pluginOptionsValidator.Validate(null, _pluginOptions.CurrentValue);
            if (!validationResult.Succeeded)
            {
                throw new OptionsValidationException(nameof(PluginOptions), typeof(PluginOptions), validationResult.Failures ?? new[] { validationResult.FailureMessage ?? "Unknown validation error" });
            }
            
            // 监听插件配置更改
            _pluginOptions.OnChange(async (options, name) =>
            {
                try
                {
                    var result = _pluginOptionsValidator.Validate(name, options);
                    if (result.Succeeded)
                    {
                        await LoadPluginsAsync(options.InstallPath);
                    }
                    else
                    {
                        Console.WriteLine($"Plugin configuration validation failed: {string.Join(", ", result.Failures ?? new[] { "Unknown validation error"})}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error handling plugin configuration change: {ex.Message}");
                }
            });
        }

        /// <summary>
        /// 确保插件目录存在
        /// </summary>
        private void EnsurePluginDirectories(PluginOptions options)
        {
            var installPath = Environment.ExpandEnvironmentVariables(options.InstallPath);
            var userPath = Environment.ExpandEnvironmentVariables(options.UserPath);

            Directory.CreateDirectory(installPath);
            Directory.CreateDirectory(userPath);
        }

        /// <summary>
        /// 异步加载插件
        /// </summary>
        /// <param name="pluginsPath">插件路径</param>
        /// <returns>异步任务</returns>
        public async Task<IEnumerable<IPlugin>> LoadPluginsAsync(string pluginsPath)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var options = _pluginOptions.CurrentValue;
                    
                    // 1. 软件自带插件目录（安装目录）
                    string installPluginsPath = Path.Combine(AppContext.BaseDirectory, options.InstallPath);
                    // 2. 用户自定义插件目录
                    string userPluginsPath = Environment.ExpandEnvironmentVariables(options.UserPath);

                    Directory.CreateDirectory(installPluginsPath); // Ensure directories exist
                    Directory.CreateDirectory(userPluginsPath);

                    var allPluginDirs = new List<string>();
                    if (Directory.Exists(installPluginsPath)) allPluginDirs.Add(installPluginsPath);
                    if (Directory.Exists(userPluginsPath)) allPluginDirs.Add(userPluginsPath);
                    // 兼容旧参数
                    if (!string.IsNullOrWhiteSpace(pluginsPath) && Directory.Exists(pluginsPath) && !allPluginDirs.Contains(pluginsPath))
                        allPluginDirs.Add(pluginsPath);

                    LoadedPlugins.Clear();
                    // Reset services collection for new plugin loading cycle
                    // This is a simple approach; more sophisticated DI might involve child containers per plugin.
                    _services.Clear(); 
                    _services.AddSingleton(_configuration);
                    // Add any other core services that plugins might depend on, if not already present or if cleared.

                    foreach (var dir in allPluginDirs)
                    {
                        var pluginFiles = Directory.GetFiles(dir, "*.dll", SearchOption.AllDirectories);
                        foreach (var pluginFile in pluginFiles)
                        {
                            LoadPluginFromFile(pluginFile, _services);
                        }
                    }

                    _serviceProvider = _services.BuildServiceProvider();

                    // 初始化所有加载的插件
                    foreach (var plugin in LoadedPlugins)
                    {
                        try
                        {
                            plugin.Initialize(_serviceProvider);
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error initializing plugin {plugin.GetMetadata()?.Name}: {ex.Message}");
                        }
                    }

                    // 为所有已加载的插件添加导航菜单项
                    _navigationPluginService.AddPluginNavigationItems(LoadedPlugins);

                    // 返回已加载的插件列表
                    return LoadedPlugins.AsEnumerable();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"插件加载流程发生异常: {ex.Message}");
                    return Enumerable.Empty<IPlugin>();
                }
            });
        }

        private void LoadPluginFromFile(string pluginFile, IServiceCollection services)
        {
            try
            {
                var assembly = System.Runtime.Loader.AssemblyLoadContext.Default.LoadFromAssemblyPath(pluginFile);
                foreach (var type in assembly.GetTypes())
                {
                    if (typeof(IPlugin).IsAssignableFrom(type) && !type.IsAbstract)
                    {
                        var plugin = Activator.CreateInstance(type) as IPlugin;
                        if (plugin == null)
                        {
                            System.Diagnostics.Debug.WriteLine($"Could not create plugin instance: {type.FullName}");
                            continue;
                        }

                        var meta = plugin.GetMetadata();
                        if (meta == null)
                        {
                            System.Diagnostics.Debug.WriteLine($"插件元数据为空: {type.FullName}");
                            continue;
                        }
                        
                        if (LoadedPlugins.Any(p => p.GetMetadata().Name == meta.Name))
                        {
                             System.Diagnostics.Debug.WriteLine($"Plugin {meta.Name} is already processed in this batch.");
                             continue; // Avoid duplicate processing if found multiple times in scan
                        }

                        System.Diagnostics.Debug.WriteLine($"加载插件: {meta.Name} v{meta.Version}");

                        // 配置插件服务
                        plugin.ConfigureServices(services, _configuration);

                        // 添加到已加载插件列表
                        LoadedPlugins.Add(plugin);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载插件 {pluginFile} 时出错: {ex.Message}");
            }
        }

        public async Task<IPlugin?> LoadPluginAsync(string pluginFilePath)
        {
            return await Task.Run(() =>
            {
                if (string.IsNullOrEmpty(pluginFilePath) || !File.Exists(pluginFilePath))
                {
                    Console.WriteLine($"Plugin file not found: {pluginFilePath}");
                    return null;
                }

                try
                {
                    var assembly = System.Runtime.Loader.AssemblyLoadContext.Default.LoadFromAssemblyPath(pluginFilePath);
                    foreach (var type in assembly.GetTypes())
                    {
                        if (typeof(IPlugin).IsAssignableFrom(type) && !type.IsAbstract)
                        {
                            var plugin = Activator.CreateInstance(type) as IPlugin;
                            if (plugin == null) continue;

                            var meta = plugin.GetMetadata();
                            if (meta == null) continue;
                            
                            if (LoadedPlugins.Any(p => p.GetMetadata().Name == meta.Name))
                            {
                                Console.WriteLine($"Plugin {meta.Name} is already loaded.");
                                return LoadedPlugins.First(p => p.GetMetadata().Name == meta.Name);
                            }

                            // Create a temporary service collection to see what services the plugin wants to add.
                            // This is complex if services are singletons or have other lifetime considerations
                            // that conflict with the main container. A true isolated DI per plugin is better.
                            var pluginServiceCollection = new ServiceCollection();
                            plugin.ConfigureServices(pluginServiceCollection, _configuration);
                            
                            // For simplicity, we're assuming plugins can add to the main _services collection
                            // and then we rebuild the _serviceProvider. This has limitations.
                            foreach(var descriptor in pluginServiceCollection)
                            {
                                _services.Add(descriptor);
                            }
                            _serviceProvider = _services.BuildServiceProvider(); // Rebuild after adding new services
                            
                            plugin.Initialize(_serviceProvider);
                            LoadedPlugins.Add(plugin);
                            _navigationPluginService.AddPluginNavigationItems(new List<IPlugin> { plugin });
                            Console.WriteLine($"Loaded plugin: {meta.Name} v{meta.Version}");
                            return plugin;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading plugin {pluginFilePath}: {ex.Message}");
                    return null;
                }
                return null;
            });
        }

        public async Task<bool> UnloadPluginAsync(string pluginId)
        {
            return await Task.Run(() =>
            {
                var pluginToUnload = LoadedPlugins.FirstOrDefault(p => p.GetMetadata().Name.Equals(pluginId, StringComparison.OrdinalIgnoreCase));
                if (pluginToUnload != null)
                {
                    try
                    {
                        // Basic unloading: remove from list and navigation
                        // Proper unloading in .NET with AssemblyLoadContext is complex and involves ensuring no references are held to the plugin's types or assembly.
                        // This example is highly simplified.
                        _navigationPluginService.RemovePluginNavigationItems(pluginToUnload.GetMetadata().Name);
                        LoadedPlugins.Remove(pluginToUnload);
                        
                        Console.WriteLine($"Unloaded plugin: {pluginId}");
                        return true;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error unloading plugin {pluginId}: {ex.Message}");
                        return false;
                    }
                }
                Console.WriteLine($"Plugin not found for unload: {pluginId}");
                return false;
            });
        }

        public bool IsPluginLoaded(string pluginId)
        {
            return LoadedPlugins.Any(p => p.GetMetadata().Name.Equals(pluginId, StringComparison.OrdinalIgnoreCase));
        }
    }
}
