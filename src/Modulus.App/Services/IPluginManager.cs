using System.Collections.Generic;
using System.Threading.Tasks;
using Modulus.Plugin.Abstractions;

namespace Modulus.App.Services
{
    /// <summary>
    /// 插件管理器接口
    /// </summary>
    public interface IPluginManager
    {
        /// <summary>
        /// 已加载的插件列表
        /// </summary>
        List<IPlugin> LoadedPlugins { get; }
        
        /// <summary>
        /// 异步加载插件
        /// </summary>
        /// <param name="pluginsPath">插件路径</param>
        /// <returns>加载的插件列表</returns>
        Task<IEnumerable<IPlugin>> LoadPluginsAsync(string pluginsPath);
    }
} 