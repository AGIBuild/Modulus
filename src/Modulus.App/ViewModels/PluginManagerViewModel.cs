using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Modulus.App.Services;
using System;
using System.Collections.ObjectModel;
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
        private string statusMessage = string.Empty;

        /// <summary>
        /// 创建插件管理器视图模型
        /// </summary>
        public PluginManagerViewModel(INavigationService navigationService, IPluginManager pluginManager) 
            : base(navigationService)
        {
            _pluginManager = pluginManager ?? throw new ArgumentNullException(nameof(pluginManager));
            
            // 添加一些示例插件数据
            AddSamplePlugins();
        }

        /// <summary>
        /// 添加示例插件数据
        /// </summary>
        private void AddSamplePlugins()
        {
            Plugins.Clear();
            RecentlyUpdatedPlugins.Clear();

            // 添加一些示例插件
            var samplePlugins = new[]
            {
                new PluginViewModel
                {
                    PluginName = "数据分析插件",
                    PluginVersion = "1.2.3",
                    PluginAuthor = "数据团队",
                    PluginDescription = "提供数据分析和可视化功能",
                    PluginIsEnabled = true,
                    PluginStatus = "已启用"
                },
                new PluginViewModel
                {
                    PluginName = "报表生成器",
                    PluginVersion = "2.0.1",
                    PluginAuthor = "报表团队",
                    PluginDescription = "支持导出各种格式的报表",
                    PluginIsEnabled = true,
                    PluginStatus = "已启用"
                },
                new PluginViewModel
                {
                    PluginName = "文档处理工具",
                    PluginVersion = "0.9.5",
                    PluginAuthor = "文档团队",
                    PluginDescription = "处理各种文档格式",
                    PluginIsEnabled = false,
                    PluginStatus = "已禁用"
                }
            };

            foreach (var plugin in samplePlugins)
            {
                Plugins.Add(plugin);
                
                // 假设前两个插件是最近更新的
                if (plugin.PluginName == "数据分析插件" || plugin.PluginName == "报表生成器")
                {
                    RecentlyUpdatedPlugins.Add(plugin);
                }
            }
        }

        /// <summary>
        /// 安装插件命令
        /// </summary>
        [RelayCommand]
        private async Task InstallPluginFromFile()
        {
            IsLoading = true;
            StatusMessage = "正在安装插件...";

            // 在实际实现中，这里会显示文件对话框
            // 并安装选定的插件
            await Task.Delay(1000); // 模拟安装延迟

            // 刷新插件列表
            AddSamplePlugins();

            IsLoading = false;
            StatusMessage = "插件安装成功。";
        }

        /// <summary>
        /// 刷新插件命令
        /// </summary>
        [RelayCommand]
        private void RefreshPlugins()
        {
            AddSamplePlugins();
            StatusMessage = "插件已刷新。";
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
