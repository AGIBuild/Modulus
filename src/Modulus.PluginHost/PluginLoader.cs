using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Loader;
using Modulus.Plugin.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Modulus.PluginHost
{
    public class PluginLoader
    {
        public static string GetUserPluginDirectory()
        {
            var userDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var pluginDir = Path.Combine(userDir, ".modulus", "plugins");
            return pluginDir;
        }

        public event EventHandler? PluginReloaded;

        private readonly Dictionary<string, (AssemblyLoadContext Context, WeakReference Ref)> _loadedPlugins = new();
        private readonly ILogger<PluginLoader>? _logger;

        public PluginLoader(ILogger<PluginLoader>? logger = null)
        {
            _logger = logger;
        }

        /// <summary>
        /// Creates an isolated AssemblyLoadContext for loading plugin assemblies
        /// </summary>
        public AssemblyLoadContext LoadPlugin(string pluginPath)
        {
            _logger?.LogDebug("Creating isolated load context for plugin: {PluginPath}", pluginPath);
            
            // Create a dedicated AssemblyLoadContext with a unique name based on the plugin filename
            var pluginName = Path.GetFileNameWithoutExtension(pluginPath);
            var context = new PluginAssemblyLoadContext(pluginName, isCollectible: true);
            
            return context;
        }

        /// <summary>
        /// Unloads a plugin's AssemblyLoadContext and releases all references
        /// </summary>
        public void UnloadPlugin(AssemblyLoadContext context)
        {
            _logger?.LogDebug("Unloading plugin context: {ContextName}", context.Name);
            context.Unload();
        }

        /// <summary>
        /// Sets up file system watcher to monitor changes in the plugin directory
        /// </summary>
        public void WatchPlugins()
        {
            var pluginDir = GetUserPluginDirectory();
            Directory.CreateDirectory(pluginDir);
            
            _logger?.LogInformation("Watching plugin directory: {PluginDir}", pluginDir);
            
            var watcher = new FileSystemWatcher(pluginDir, "*.dll")
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.CreationTime,
                EnableRaisingEvents = true,
                IncludeSubdirectories = false
            };
            
            watcher.Changed += (s, e) => {
                _logger?.LogDebug("Plugin file changed: {PluginFile}", e.FullPath);
                PluginReloaded?.Invoke(this, EventArgs.Empty);
            };
            watcher.Created += (s, e) => {
                _logger?.LogDebug("Plugin file created: {PluginFile}", e.FullPath);
                PluginReloaded?.Invoke(this, EventArgs.Empty);
            };
            watcher.Deleted += (s, e) => {
                _logger?.LogDebug("Plugin file deleted: {PluginFile}", e.FullPath);
                PluginReloaded?.Invoke(this, EventArgs.Empty);
            };
            watcher.Renamed += (s, e) => {
                _logger?.LogDebug("Plugin file renamed: {OldName} to {NewName}", e.OldFullPath, e.FullPath);
                PluginReloaded?.Invoke(this, EventArgs.Empty);
            };
        }

        /// <summary>
        /// Discovers plugin DLLs in the user's plugin directory
        /// </summary>
        public IEnumerable<string> DiscoverPlugins()
        {
            var pluginDir = GetUserPluginDirectory();
            if (!Directory.Exists(pluginDir))
                yield break;
                
            _logger?.LogInformation("Discovering plugins in {PluginDir}", pluginDir);
            
            foreach (var dll in Directory.GetFiles(pluginDir, "*.dll", SearchOption.TopDirectoryOnly))
            {
                yield return dll;
            }
        }

        /// <summary>
        /// Reads plugin metadata from associated JSON file
        /// </summary>
        public PluginMeta? ReadMeta(string pluginPath)
        {
            var metaPath = Path.ChangeExtension(pluginPath, ".json");
            if (!File.Exists(metaPath))
            {
                _logger?.LogWarning("Plugin metadata not found for {PluginPath}", pluginPath);
                return null;
            }
            
            try
            {
                var json = File.ReadAllText(metaPath);
                var meta = System.Text.Json.JsonSerializer.Deserialize<PluginMeta>(json);
                _logger?.LogDebug("Loaded plugin metadata for {PluginName} v{Version}", meta?.Name, meta?.Version);
                return meta;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error reading plugin metadata for {PluginPath}", pluginPath);
                return null;
            }
        }

        /// <summary>
        /// Runs a plugin based on metadata EntryPoint (legacy approach)
        /// </summary>
        public object? RunPlugin(string pluginPath)
        {
            _logger?.LogInformation("Loading plugin: {PluginPath}", pluginPath);
            
            var meta = ReadMeta(pluginPath);
            if (meta?.EntryPoint == null)
                throw new InvalidOperationException("Plugin entry point not defined.");
                
            try
            {
                var context = LoadPlugin(pluginPath);
                var asm = context.LoadFromAssemblyPath(pluginPath);
                var type = asm.GetType(meta.EntryPoint);
                
                if (type == null)
                    throw new InvalidOperationException($"Entry point type '{meta.EntryPoint}' not found.");
                    
                var instance = Activator.CreateInstance(type);
                _loadedPlugins[pluginPath] = (context, new WeakReference(context));
                
                _logger?.LogInformation("Successfully loaded plugin: {PluginName}", meta.Name);
                return instance;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to load plugin: {PluginPath}", pluginPath);
                throw;
            }
        }

        /// <summary>
        /// Runs a plugin in a sandboxed environment with interface validation
        /// </summary>
        public object? RunPluginSandboxed(string pluginPath, Type pluginInterface)
        {
            _logger?.LogInformation("Loading sandboxed plugin: {PluginPath}", pluginPath);
            
            var meta = ReadMeta(pluginPath);
            if (meta?.EntryPoint == null)
                throw new InvalidOperationException("Plugin entry point not defined.");
                
            try
            {
                var context = LoadPlugin(pluginPath);
                var asm = context.LoadFromAssemblyPath(pluginPath);
                var type = asm.GetType(meta.EntryPoint);
                
                if (type == null)
                    throw new InvalidOperationException($"Entry point type '{meta.EntryPoint}' not found.");
                    
                if (!pluginInterface.IsAssignableFrom(type))
                {
                    _logger?.LogError("Plugin type {PluginType} does not implement {Interface}", type.FullName, pluginInterface.FullName);
                    throw new InvalidOperationException($"Plugin must implement {pluginInterface.Name} interface.");
                }
                
                var instance = Activator.CreateInstance(type);
                _loadedPlugins[pluginPath] = (context, new WeakReference(context));
                
                _logger?.LogInformation("Successfully loaded sandboxed plugin: {PluginName}", meta.Name);
                return instance;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to load sandboxed plugin: {PluginPath}", pluginPath);
                throw;
            }
        }

        /// <summary>
        /// Safely runs a plugin with exception isolation to prevent app crashes
        /// </summary>
        public object? SafeRunPlugin(string pluginPath, Type pluginInterface)
        {
            try
            {
                return RunPluginSandboxed(pluginPath, pluginInterface);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error running plugin {PluginPath} safely", pluginPath);
                return null;
            }
        }

        /// <summary>
        /// Stops a plugin and unloads its context
        /// </summary>
        public void StopPlugin(string pluginPath)
        {
            _logger?.LogInformation("Stopping plugin: {PluginPath}", pluginPath);
            
            if (_loadedPlugins.TryGetValue(pluginPath, out var tuple))
            {
                UnloadPlugin(tuple.Context);
                _loadedPlugins.Remove(pluginPath);
            }
        }

        /// <summary>
        /// Checks if a plugin has been properly unloaded
        /// </summary>
        public bool IsPluginUnloaded(string pluginPath)
        {
            if (_loadedPlugins.TryGetValue(pluginPath, out var tuple))
            {
                return !tuple.Ref.IsAlive;
            }
            return true;
        }

        /// <summary>
        /// 加载插件并进行契约版本检查，确保只加载实现 IPlugin 接口的程序集
        /// </summary>
        public object? RunPluginWithContractCheck(string pluginPath)
        {
            _logger?.LogInformation("Loading plugin with contract check: {PluginPath}", pluginPath);
            
            var meta = ReadMeta(pluginPath);
            if (meta == null)
                throw new InvalidOperationException("Plugin metadata not found.");

            const string HostContractVersion = "2.0.0";
            const string HostMinSupportedVersion = "1.0.0";
            Version pluginVer = new Version(meta.ContractVersion);
            Version hostVer = new Version(HostContractVersion);
            Version hostMin = new Version(HostMinSupportedVersion);

            // Validate contract compatibility
            if (pluginVer < hostMin)
            {
                _logger?.LogError("Plugin {PluginName} is too old (contract v{ContractVersion})", meta.Name, meta.ContractVersion);
                throw new InvalidOperationException($"The plugin '{meta.Name}' is too old and not compatible with this version of Modulus. Please contact the plugin developer to update the plugin.");
            }
            
            if (pluginVer > hostVer)
            {
                _logger?.LogError("Plugin {PluginName} requires newer host version (contract v{ContractVersion})", meta.Name, meta.ContractVersion);
                throw new InvalidOperationException($"The plugin '{meta.Name}' requires a newer version of Modulus. Please update the application to use this plugin.");
            }

            try
            {
                var context = LoadPlugin(pluginPath);
                var asm = context.LoadFromAssemblyPath(pluginPath);

                // 查找实现 IPlugin 接口的非抽象类
                var pluginType = asm.GetTypes().FirstOrDefault(t =>
                    typeof(Modulus.Plugin.Abstractions.IPlugin).IsAssignableFrom(t) &&
                    !t.IsAbstract &&
                    !t.IsInterface);

                if (pluginType == null)
                {
                    _logger?.LogError("No valid IPlugin implementation found in assembly: {PluginPath}", pluginPath);
                    throw new InvalidOperationException("No valid IPlugin implementation found in assembly.");
                }

                var instance = Activator.CreateInstance(pluginType);
                _loadedPlugins[pluginPath] = (context, new WeakReference(context));
                
                _logger?.LogInformation("Successfully loaded plugin with contract check: {PluginName} v{Version}", meta.Name, meta.Version);
                return instance;
            }
            catch (Exception ex) when (!(ex is InvalidOperationException))
            {
                _logger?.LogError(ex, "Error loading plugin with contract check: {PluginPath}", pluginPath);
                throw;
            }
        }

        /// <summary>
        /// 加载插件并支持依赖注入、配置与日志记录
        /// </summary>
        public object? RunPluginWithDI(string pluginPath, IServiceCollection services, IConfiguration configuration, IServiceProvider? appProvider = null)
        {
            _logger?.LogInformation("Loading plugin with DI support: {PluginPath}", pluginPath);
            
            var meta = ReadMeta(pluginPath);
            if (meta == null)
                throw new InvalidOperationException("Plugin metadata not found.");

            const string HostContractVersion = "2.0.0";
            const string HostMinSupportedVersion = "1.0.0";
            Version pluginVer = new Version(meta.ContractVersion);
            Version hostVer = new Version(HostContractVersion);
            Version hostMin = new Version(HostMinSupportedVersion);

            // Validate contract compatibility
            if (pluginVer < hostMin)
            {
                _logger?.LogError("Plugin {PluginName} is too old (contract v{ContractVersion})", meta.Name, meta.ContractVersion);
                throw new InvalidOperationException($"The plugin '{meta.Name}' is too old and not compatible with this version of Modulus. Please contact the plugin developer to update the plugin.");
            }
            
            if (pluginVer > hostVer)
            {
                _logger?.LogError("Plugin {PluginName} requires newer host version (contract v{ContractVersion})", meta.Name, meta.ContractVersion);
                throw new InvalidOperationException($"The plugin '{meta.Name}' requires a newer version of Modulus. Please update the application to use this plugin.");
            }

            try
            {
                var context = LoadPlugin(pluginPath);
                var asm = context.LoadFromAssemblyPath(pluginPath);

                // 查找实现 IPlugin 接口的非抽象类
                var pluginType = asm.GetTypes().FirstOrDefault(t =>
                    typeof(Modulus.Plugin.Abstractions.IPlugin).IsAssignableFrom(t) &&
                    !t.IsAbstract &&
                    !t.IsInterface);

                if (pluginType == null)
                {
                    _logger?.LogError("No valid IPlugin implementation found in assembly: {PluginPath}", pluginPath);
                    throw new InvalidOperationException("No valid IPlugin implementation found in assembly.");
                }

                var instance = Activator.CreateInstance(pluginType);

                // 获取插件所在目录
                var pluginDir = Path.GetDirectoryName(pluginPath);

                // 为插件添加本地化服务
                if (pluginDir != null)
                {
                    services.AddSingleton<ILocalizer>(provider =>
                        new PluginLocalizer(pluginDir, provider.GetService<ILogger<PluginLocalizer>>()));
                }

                // 使用接口直接调用插件的 ConfigureServices/Initialize 方法
                if (instance is IPlugin plugin)
                {
                    _logger?.LogDebug("Configuring plugin services for {PluginName}", meta.Name);
                    plugin.ConfigureServices(services, configuration);

                    var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = false, ValidateScopes = false });
                    
                    _logger?.LogDebug("Initializing plugin {PluginName}", meta.Name);
                    plugin.Initialize(provider);
                }
                else
                {
                    // 回退到使用反射调用 ConfigureServices/Initialize 方法
                    _logger?.LogWarning("Plugin does not implement IPlugin interface properly, using reflection fallback");
                    var configureServices = pluginType.GetMethod("ConfigureServices");
                    var initialize = pluginType.GetMethod("Initialize");

                    configureServices?.Invoke(instance, new object[] { services, configuration });

                    var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = false, ValidateScopes = false });
                    initialize?.Invoke(instance, new object[] { provider });
                }

                _loadedPlugins[pluginPath] = (context, new WeakReference(context));
                
                _logger?.LogInformation("Successfully loaded plugin with DI: {PluginName} v{Version}", meta.Name, meta.Version);
                return instance;
            }
            catch (Exception ex) when (!(ex is InvalidOperationException))
            {
                _logger?.LogError(ex, "Error loading plugin with DI: {PluginPath}", pluginPath);
                throw;
            }
        }
        
        /// <summary>
        /// Runs all the discovered plugins with DI support
        /// </summary>
        public IEnumerable<object> RunAllPlugins(IServiceCollection services, IConfiguration configuration)
        {
            _logger?.LogInformation("Loading all discovered plugins");
            var results = new List<object>();
            
            foreach (var pluginPath in DiscoverPlugins())
            {
                try
                {
                    var instance = RunPluginWithDI(pluginPath, services, configuration);
                    if (instance != null)
                    {
                        results.Add(instance);
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Failed to load plugin {PluginPath}", pluginPath);
                    // Continue with other plugins
                }
            }
            
            _logger?.LogInformation("Loaded {Count} plugins successfully", results.Count);
            return results;
        }
    }

    /// <summary>
    /// Custom AssemblyLoadContext for plugin isolation
    /// </summary>
    internal class PluginAssemblyLoadContext : AssemblyLoadContext
    {
        private readonly AssemblyDependencyResolver _resolver;

        public PluginAssemblyLoadContext(string pluginName, bool isCollectible = true) 
            : base(pluginName, isCollectible)
        {
            // Create a resolver for the plugin directory to help resolve plugin dependencies
            _resolver = new AssemblyDependencyResolver(
                Path.Combine(
                    PluginLoader.GetUserPluginDirectory(),
                    pluginName));
        }

        protected override System.Reflection.Assembly? Load(System.Reflection.AssemblyName assemblyName)
        {
            // First, try to resolve the assembly using the plugin's dependency resolver
            string? assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
            
            if (assemblyPath != null)
            {
                return LoadFromAssemblyPath(assemblyPath);
            }

            // Fall back to default loading behavior
            return null;
        }

        protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
        {
            // Try to resolve unmanaged DLLs using the plugin's dependency resolver
            string? libraryPath = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
            
            if (libraryPath != null)
            {
                return LoadUnmanagedDllFromPath(libraryPath);
            }

            // Fall back to default loading behavior
            return IntPtr.Zero;
        }
    }

    public class PluginMeta
    {
        public string? Name { get; set; }
        public string? Version { get; set; }
        public string? Description { get; set; }
        public string? Author { get; set; }
        public string? EntryPoint { get; set; }
        public string[]? Dependencies { get; set; }
        public string ContractVersion { get; set; } = "1.0.0";
        public string? NavigationIcon { get; set; }
        public string? NavigationSection { get; set; }
        public int NavigationOrder { get; set; } = 100;
    }
}
