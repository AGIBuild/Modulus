using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Modulus.App.Services;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;

namespace Modulus.App.ViewModels
{
    /// <summary>
    /// ViewModel for the Plugin Manager view.
    /// </summary>
    public partial class PluginManagerViewModel : NavigationViewModelBase
    {
        private readonly IPluginManager _pluginManager;

        /// <summary>
        /// 视图名称
        /// </summary>
        public override string ViewName => "PluginManagerView";

        /// <summary>
        /// 插件列表
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<PluginViewModel> plugins = new();

        /// <summary>
        /// 选中的插件
        /// </summary>
        [ObservableProperty]
        private PluginViewModel? selectedPlugin;

        /// <summary>
        /// 搜索文本
        /// </summary>
        [ObservableProperty]
        private string searchText = string.Empty;

        /// <summary>
        /// 最近更新的插件
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<PluginViewModel> recentlyUpdatedPlugins = new();

        /// <summary>
        /// 是否正在加载
        /// </summary>
        [ObservableProperty]
        private bool isLoading;

        /// <summary>
        /// 状态消息
        /// </summary>
        [ObservableProperty]
        private string statusMessage = string.Empty;        /// <summary>
        /// 创建插件管理器视图模型
        /// </summary>
        public PluginManagerViewModel(INavigationService navigationService, IPluginManager pluginManager) 
            : base(navigationService)
        {
            _pluginManager = pluginManager ?? throw new ArgumentNullException(nameof(pluginManager));
            
            // 初始化插件列表
            RefreshPluginsList();
        }/// <summary>
        /// 刷新插件列表
        /// </summary>
        private void RefreshPluginsList()
        {
            Plugins.Clear();
            RecentlyUpdatedPlugins.Clear();

            foreach (var plugin in _pluginManager.LoadedPlugins)
            {
                var meta = plugin.GetMetadata();
                var viewModel = new PluginViewModel
                {
                    PluginName = meta.Name,
                    PluginVersion = meta.Version,
                    PluginAuthor = meta.Author,
                    PluginDescription = meta.Description,
                    PluginIsEnabled = true,
                    PluginStatus = "已启用"
                };

                Plugins.Add(viewModel);
                
                // 添加最近更新的插件 (基于版本号)
                if (meta.Version.EndsWith(".0"))
                {
                    RecentlyUpdatedPlugins.Add(viewModel);
                }
            }
        }        /// <summary>
        /// 安装插件命令
        /// </summary>
        [RelayCommand]
        private async Task InstallPluginFromFile()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "正在安装插件...";

                // TODO: 显示文件对话框选择插件文件
                // 为了演示，我们暂时使用内置的示例插件目录
                var pluginsPath = Path.Combine(AppContext.BaseDirectory, "plugins");
                
                // 加载新插件
                await _pluginManager.LoadPluginsAsync(pluginsPath);

                // 刷新插件列表UI
                RefreshPluginsList();

                StatusMessage = "插件安装成功。";
            }
            catch (Exception ex)
            {
                StatusMessage = $"安装插件失败: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }/// <summary>
        /// 刷新插件命令
        /// </summary>
        [RelayCommand]
        private void RefreshPlugins()
        {
            try
            {
                RefreshPluginsList();
                StatusMessage = "插件已刷新。";
            }
            catch (Exception ex)
            {
                StatusMessage = $"刷新插件失败: {ex.Message}";
            }
        }

        /// <summary>
        /// 卸载插件命令
        /// </summary>
        [RelayCommand]
        private void UninstallSelectedPlugin()
        {
            if (SelectedPlugin == null)
            {
                StatusMessage = "未选择插件。";
                return;
            }

            // 卸载选中的插件
            var pluginName = SelectedPlugin.PluginName;
            Plugins.Remove(SelectedPlugin);
            RecentlyUpdatedPlugins.Remove(SelectedPlugin);
            
            StatusMessage = $"插件'{pluginName}'已卸载。";
            SelectedPlugin = null;
        }
    }
}
