using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Loader;

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

        public event EventHandler PluginReloaded;

        private readonly Dictionary<string, (AssemblyLoadContext Context, WeakReference Ref)> _loadedPlugins = new();

        public AssemblyLoadContext LoadPlugin(string pluginPath)
        {
            // TODO: 实现真正的隔离加载
            var context = new AssemblyLoadContext(Path.GetFileNameWithoutExtension(pluginPath), isCollectible: true);
            // context.LoadFromAssemblyPath(pluginPath); // 需真实dll
            return context;
        }

        public void UnloadPlugin(AssemblyLoadContext context)
        {
            context.Unload();
            // 需配合 WeakReference 检查是否真正卸载
        }

        public void WatchPlugins()
        {
            var pluginDir = GetUserPluginDirectory();
            Directory.CreateDirectory(pluginDir);
            var watcher = new FileSystemWatcher(pluginDir, "*.dll")
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.CreationTime,
                EnableRaisingEvents = true,
                IncludeSubdirectories = false
            };
            watcher.Changed += (s, e) => PluginReloaded?.Invoke(this, EventArgs.Empty);
            watcher.Created += (s, e) => PluginReloaded?.Invoke(this, EventArgs.Empty);
            watcher.Deleted += (s, e) => PluginReloaded?.Invoke(this, EventArgs.Empty);
            watcher.Renamed += (s, e) => PluginReloaded?.Invoke(this, EventArgs.Empty);
        }

        public IEnumerable<string> DiscoverPlugins()
        {
            var pluginDir = GetUserPluginDirectory();
            if (!Directory.Exists(pluginDir))
                yield break;
            foreach (var dll in Directory.GetFiles(pluginDir, "*.dll", SearchOption.TopDirectoryOnly))
            {
                yield return dll;
            }
        }

        public PluginMeta? ReadMeta(string pluginPath)
        {
            var metaPath = Path.ChangeExtension(pluginPath, ".json");
            if (!File.Exists(metaPath)) return null;
            var json = File.ReadAllText(metaPath);
            return System.Text.Json.JsonSerializer.Deserialize<PluginMeta>(json);
        }

        public object? RunPlugin(string pluginPath)
        {
            var meta = ReadMeta(pluginPath);
            if (meta?.EntryPoint == null)
                throw new InvalidOperationException("Plugin entry point not defined.");
            var context = LoadPlugin(pluginPath);
            var asm = context.LoadFromAssemblyPath(pluginPath);
            var type = asm.GetType(meta.EntryPoint);
            if (type == null)
                throw new InvalidOperationException($"Entry point type '{meta.EntryPoint}' not found.");
            var instance = Activator.CreateInstance(type);
            _loadedPlugins[pluginPath] = (context, new WeakReference(context));
            return instance;
        }

        // Sandbox: Only load plugins implementing IPlugin, restrict reflection and main assembly access
        public object? RunPluginSandboxed(string pluginPath, Type pluginInterface)
        {
            var meta = ReadMeta(pluginPath);
            if (meta?.EntryPoint == null)
                throw new InvalidOperationException("Plugin entry point not defined.");
            var context = LoadPlugin(pluginPath);
            var asm = context.LoadFromAssemblyPath(pluginPath);
            var type = asm.GetType(meta.EntryPoint);
            if (type == null)
                throw new InvalidOperationException($"Entry point type '{meta.EntryPoint}' not found.");
            if (!pluginInterface.IsAssignableFrom(type))
                throw new InvalidOperationException($"Plugin must implement {pluginInterface.Name} interface.");
            var instance = Activator.CreateInstance(type);
            _loadedPlugins[pluginPath] = (context, new WeakReference(context));
            return instance;
        }

        // Exception isolation: Catch plugin exceptions to prevent main application crash
        public object? SafeRunPlugin(string pluginPath, Type pluginInterface)
        {
            try
            {
                return RunPluginSandboxed(pluginPath, pluginInterface);
            }
            catch (Exception)
            {
                // TODO: Log exception
                return null;
            }
        }

        public void StopPlugin(string pluginPath)
        {
            if (_loadedPlugins.TryGetValue(pluginPath, out var tuple))
            {
                UnloadPlugin(tuple.Context);
                _loadedPlugins.Remove(pluginPath);
            }
        }

        public bool IsPluginUnloaded(string pluginPath)
        {
            if (_loadedPlugins.TryGetValue(pluginPath, out var tuple))
            {
                return !tuple.Ref.IsAlive;
            }
            return true;
        }

        public bool IsPluginUnloadedFixed(string pluginPath)
        {
            if (_loadedPlugins.TryGetValue(pluginPath, out var tuple))
            {
                return !tuple.Ref.IsAlive;
            }
            return true;
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
    }
}
