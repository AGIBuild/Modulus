using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
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
        private IServiceProvider? _serviceProvider;

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
            
            // 验证和应用初始配置
            var validationResult = _pluginOptionsValidator.Validate(null, _pluginOptions.CurrentValue);
            if (!validationResult.Succeeded)
            {
                throw new OptionsValidationException(nameof(PluginOptions), typeof(PluginOptions), new[] { validationResult.FailureMessage});
            }
            
            // 监听插件配置更改
            _pluginOptions.OnChange((options, name) =>
            {
                try
                {
                    var result = _pluginOptionsValidator.Validate(name, options);
                    if (result.Succeeded)
                    {
                        // 当插件配置发生更改且验证通过时，触发重新加载
                        Task.Run(() => LoadPluginsAsync(options.InstallPath));
                    }
                    else
                    {
                        Console.WriteLine($"Plugin configuration validation failed: {string.Join(", ", result.FailureMessage)}");
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
            try
            {
                var options = _pluginOptions.CurrentValue;
                
                // 1. 软件自带插件目录（安装目录）
                string installPluginsPath = Path.Combine(AppContext.BaseDirectory, options.InstallPath);
                // 2. 用户自定义插件目录
                string userPluginsPath = Environment.ExpandEnvironmentVariables(options.UserPath);

                var allPluginDirs = new List<string>();
                if (Directory.Exists(installPluginsPath)) allPluginDirs.Add(installPluginsPath);
                if (Directory.Exists(userPluginsPath)) allPluginDirs.Add(userPluginsPath);
                // 兼容旧参数
                if (!string.IsNullOrWhiteSpace(pluginsPath) && Directory.Exists(pluginsPath) && !allPluginDirs.Contains(pluginsPath))
                    allPluginDirs.Add(pluginsPath);

                LoadedPlugins.Clear();
                _services.Clear();
                _services.AddSingleton(_configuration);

                foreach (var dir in allPluginDirs)
                {
                    var pluginFiles = Directory.GetFiles(dir, "*.dll", SearchOption.AllDirectories);
                    foreach (var pluginFile in pluginFiles)
                    {
                        try
                        {
                            // 加载插件程序集
                            var assembly = System.Runtime.Loader.AssemblyLoadContext.Default.LoadFromAssemblyPath(pluginFile);

                            // 查找实现了 IPlugin 接口的类型
                            foreach (var type in assembly.GetTypes())
                            {
                                if (typeof(Modulus.Plugin.Abstractions.IPlugin).IsAssignableFrom(type) && !type.IsAbstract)
                                {
                                    try
                                    {
                                        // 创建插件实例
                                        if (Activator.CreateInstance(type) is not IPlugin plugin)
                                        {
                                            throw new InvalidOperationException($"无法创建插件实例: {type.FullName}");
                                        }

                                        // 获取并验证插件元数据
                                        var meta = plugin.GetMetadata();
                                        if (meta == null)
                                        {
                                            throw new InvalidOperationException($"插件元数据为空: {type.FullName}");
                                        }

                                        System.Diagnostics.Debug.WriteLine($"加载插件: {meta.Name} v{meta.Version}");

                                        // 配置插件服务
                                        plugin.ConfigureServices(_services, _configuration);

                                        // 添加到已加载插件列表
                                        LoadedPlugins.Add(plugin);
                                    }
                                    catch (Exception ex)
                                    {
                                        System.Diagnostics.Debug.WriteLine($"初始化插件 {type.FullName} 时出错: {ex.Message}");
                                        System.Diagnostics.Debug.WriteLine(ex.StackTrace);
                                        continue;
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"加载插件 {pluginFile} 时出错: {ex.Message}");
                        }
                    }
                }

                // 重建服务提供器
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
                        System.Diagnostics.Debug.WriteLine($"初始化插件时出错: {ex.Message}");
                        System.Diagnostics.Debug.WriteLine(ex.StackTrace);
                    }
                }

                // 为所有已加载的插件添加导航菜单项
                _navigationPluginService.AddPluginNavigationItems(LoadedPlugins);

                // 返回已加载的插件列表
                return LoadedPlugins;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"插件加载流程发生异常: {ex.Message}");
                System.Diagnostics.Debug.WriteLine(ex.StackTrace);
                return LoadedPlugins;
            }
        }

#if DEBUG
        /// <summary>
        /// 仅在开发环境使用：复制示例插件到插件目录
        /// </summary>
        private async Task CopyExamplePluginsAsync(string pluginsPath)
        {
            // 获取示例插件目录路径
            var solutionDir = AppContext.BaseDirectory;
            while (!File.Exists(Path.Combine(solutionDir, "Modulus.sln")) && Directory.GetParent(solutionDir) != null)
            {
                solutionDir = Directory.GetParent(solutionDir)!.FullName;
            }

            var samplesDir = Path.Combine(solutionDir, "src", "samples");
            if (!Directory.Exists(samplesDir))
            {
                Console.WriteLine($"示例目录不存在: {samplesDir}");
                return;
            }

            // 复制每个示例插件的输出到插件目录
            foreach (var projectDir in Directory.GetDirectories(samplesDir))
            {
                var projectName = new DirectoryInfo(projectDir).Name;
                var binDir = Path.Combine(projectDir, "bin", "Debug", "net8.0");
                if (Directory.Exists(binDir))
                {
                    var pluginDir = Path.Combine(pluginsPath, projectName);
                    Directory.CreateDirectory(pluginDir);

                    foreach (var file in Directory.GetFiles(binDir))
                    {
                        var destFile = Path.Combine(pluginDir, Path.GetFileName(file));
                        File.Copy(file, destFile, true);
                    }
                    Console.WriteLine($"已复制示例插件: {projectName}");
                }
            }

            await Task.CompletedTask;
        }
#endif
    }
}
