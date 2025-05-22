using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Modulus.App.Services;
using Modulus.App.ViewModels;
using Modulus.App.Views;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Modulus.App;

public partial class App : Application
{
    private ServiceProvider? _serviceProvider;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        // Line below is needed to remove Avalonia data validation.
        // Without this line you will get duplicate validations from both Avalonia and CT
        BindingPlugins.DataValidators.RemoveAt(0);

        // 配置服务
        ConfigureServices();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var mainViewModel = _serviceProvider!.GetRequiredService<MainViewModel>();
            desktop.MainWindow = new MainWindow
            {
                DataContext = mainViewModel
            };
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            // 在单视图平台中，也应该使用依赖注入获取MainViewModel
            var mainViewModel = _serviceProvider!.GetRequiredService<MainViewModel>();
            singleViewPlatform.MainView = new MainView
            {
                DataContext = mainViewModel
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void ConfigureServices()
    {
        var services = new ServiceCollection();
        
        // 注册导航服务（单例）
        services.AddSingleton<INavigationService, NavigationService>();
        
        // 注册导航插件服务（单例）
        services.AddSingleton<NavigationPluginService>();
        
        // 注册插件管理器（单例）
        services.AddSingleton<IPluginManager, PluginManager>();
        
        // 注册视图模型（单例）
        services.AddSingleton<MainViewModel>();
        
        // 注册页面的ViewModel（可选择注册为单例或Transient）
        services.AddTransient<DashboardViewModel>();
        
        // 创建服务提供器
        _serviceProvider = services.BuildServiceProvider();
    }
}
