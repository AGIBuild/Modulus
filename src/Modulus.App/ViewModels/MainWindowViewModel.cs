using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Modulus.App.Controls.ViewModels;
using Modulus.App.Services;
using Modulus.App.Views;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Modulus.App.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private readonly IPluginManager _pluginManager;
    private readonly INavigationService _navigationService;
    private readonly IConfiguration _configuration;

    [ObservableProperty]
    private NavigationViewModel navigation;

    public MainWindowViewModel(
        IPluginManager pluginManager,
        INavigationService navigationService,
        NavigationPluginService navigationPluginService,
        IConfiguration configuration)
    {
        // 设置服务
        _pluginManager = pluginManager ?? throw new ArgumentNullException(nameof(pluginManager));
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

        // 初始化导航视图模型
        navigation = new NavigationViewModel(_navigationService);

        // 将导航视图模型关联到导航服务
        _navigationService.SetNavigationViewModel(navigation);

        // 设置导航插件服务的主视图模型
        navigationPluginService.SetMainViewModel(this);

        // 配置导航菜单
        InitializeNavigationMenu();

        // 导航到默认页面
        _navigationService.NavigateTo("DashboardView");

        // 后台加载插件
        Task.Run(async () => await LoadPluginsAsync());
    }

    /// <summary>
    /// 初始化导航菜单
    /// </summary>
    private void InitializeNavigationMenu()
    {
        // 添加主导航项
        Navigation.AddNavigationItem("仪表盘", "\uE80F", "DashboardView");
        Navigation.AddNavigationItem("插件管理", "\uE7FC", "PluginManagerView");
        Navigation.AddNavigationItem("设置", "\uE713", "SettingsView", "footer");

        // 为某些导航项添加徽章
        var notificationsItem = Navigation.AddNavigationItem("通知", "\uE7E7", "NotificationsView");
        notificationsItem.SetBadge(3);

        var helpItem = Navigation.AddNavigationItem("帮助", "\uE897", "HelpView", "footer");
        helpItem.SetDotBadge();
    }

    /// <summary>
    /// 设置活动状态
    /// </summary>
    public void SetActive(string viewName)
    {
        Navigation.SetActiveNavigationItem(viewName);
    }

    /// <summary>
    /// 加载插件
    /// </summary>
    private async Task LoadPluginsAsync()
    {
        try
        {
            // Get plugins path from configuration or use default
            var pluginsPath = _configuration.GetValue<string>("PluginsPath") ??
                             Path.Combine(AppContext.BaseDirectory, "plugins");

            // 确保插件目录存在
            if (!Directory.Exists(pluginsPath))
            {
                Directory.CreateDirectory(pluginsPath);
            }

            // 加载插件
            var loadedPlugins = await _pluginManager.LoadPluginsAsync(pluginsPath);

            // Register plugins in navigation service
            foreach (var plugin in loadedPlugins)
            {
                var meta = plugin.GetMetadata();
                if (meta == null)
                {
                    System.Diagnostics.Debug.WriteLine($"Plugin metadata is null");
                    continue;
                }

                // Create plugin container view model
                var containerVm = new PluginContainerViewModel
                {
                    PluginName = meta.Name,
                    PluginView = plugin.GetMainView()
                };

                // Register the view model for navigation
                var viewName = $"Plugin_{meta.Name}";
                _navigationService.RegisterViewModel(viewName, () => containerVm);
            }
        }
        catch (Exception ex)
        {
            // Log error properly
            System.Diagnostics.Debug.WriteLine($"Error loading plugins: {ex.Message}");
            System.Diagnostics.Debug.WriteLine(ex.StackTrace);

            // Set error status in UI
            Navigation.StatusMessage = $"Error loading plugins: {ex.Message}";
        }
    }
}

