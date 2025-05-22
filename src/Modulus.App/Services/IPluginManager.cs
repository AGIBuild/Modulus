using System.Threading.Tasks;

namespace Modulus.App.Services
{
    /// <summary>
    /// 插件管理器接口
    /// </summary>
    public interface IPluginManager
    {
        /// <summary>
        /// 异步加载插件
        /// </summary>
        /// <param name="pluginsPath">插件路径</param>
        /// <returns>异步任务</returns>
        Task LoadPluginsAsync(string pluginsPath);
        
        /// <summary>
        /// 添加测试插件（用于开发和测试环境）
        /// </summary>
        void AddTestPlugins();
    }
} 