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
        /// 异步加载指定目录中的所有插件
        /// </summary>
        /// <param name="pluginsPath">插件路径</param>
        /// <returns>加载的插件列表</returns>
        Task<IEnumerable<IPlugin>> LoadPluginsAsync(string pluginsPath);

        /// <summary>
        /// 异步加载单个插件文件
        /// </summary>
        /// <param name="pluginFilePath">插件文件路径</param>
        /// <returns>加载的插件，如果加载失败则返回 null</returns>
        Task<IPlugin?> LoadPluginAsync(string pluginFilePath);

        /// <summary>
        /// 异步卸载插件
        /// </summary>
        /// <param name="pluginId">要卸载的插件的ID (通常是插件名称)</param>
        /// <returns>如果成功卸载则返回 true，否则返回 false</returns>
        Task<bool> UnloadPluginAsync(string pluginId);

        /// <summary>
        /// 检查插件是否已加载
        /// </summary>
        /// <param name="pluginId">插件ID (通常是插件名称)</param>
        /// <returns>如果插件已加载则返回 true，否则返回 false</returns>
        bool IsPluginLoaded(string pluginId);
    }
}