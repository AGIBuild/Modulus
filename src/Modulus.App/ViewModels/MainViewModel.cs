using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Modulus.App.Controls.ViewModels;
using Modulus.App.Services;
using Modulus.App.Views;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Modulus.App.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IPluginManager _pluginManager;
    private readonly INavigationService _navigationService;

    [ObservableProperty]
    private NavigationViewModel navigation;

    public MainViewModel(
        IPluginManager pluginManager,
        INavigationService navigationService)
    {
        // 设置服务
        _pluginManager = pluginManager;
        _navigationService = navigationService;

        // 初始化导航视图模型
        navigation = new NavigationViewModel(_navigationService);

        // 将导航视图模型关联到导航服务
        _navigationService.SetNavigationViewModel(navigation);

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
        Navigation.AddNavigationItem("设置", "\uE713", "SettingsView", "footer");
        Navigation.AddNavigationItem("关于", "\uE946", "AboutView", "footer");

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
            // 在实际应用中，这里应该使用配置设置
            const string pluginsPath = @"c:\FileStorage\Projects\Modulus\tools\modulus-plugin";

            // 加载简单插件示例用于测试
            await _pluginManager.LoadPluginsAsync(pluginsPath);

            // 用于测试，手动添加测试插件
            _pluginManager.AddTestPlugins();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading plugins: {ex.Message}");
        }
    }
}

// 占位 ViewModel
public class NotificationsPlaceholderViewModel { }
public class SettingsPlaceholderViewModel { }
public class ProfilePlaceholderViewModel { }
