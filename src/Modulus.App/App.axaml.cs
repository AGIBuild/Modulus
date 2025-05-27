using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Modulus.App.Services;
using Modulus.App.ViewModels;
using Modulus.App.Views;
using Modulus.App.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System;
using System.IO;
using Serilog;
using Serilog.Events;

namespace Modulus.App;

public partial class App : Application
{
    private ServiceProvider? _serviceProvider;
    private IConfiguration? _configuration;
    private ConfigurationChangeListener? _configChangeListener;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        try
        {
            // Line below is needed to remove Avalonia data validation.
            // Without this line you will get duplicate validations from both Avalonia and CT
            BindingPlugins.DataValidators.RemoveAt(0);

            // Configure Serilog
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                .Enrich.FromLogContext()
                .WriteTo.Debug()
                .WriteTo.File(Path.Combine(AppContext.BaseDirectory, "logs", "modulus-.log"), rollingInterval: RollingInterval.Day)
                // Add other sinks like remote service if needed
                .CreateLogger();
            
            Log.Information("Application Starting...");

            // 配置服务
            ConfigureServices();

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                if (_serviceProvider == null) 
                {
                    // Handle the case where _serviceProvider is null, perhaps log an error or throw an exception
                    // For now, let's throw an exception as this indicates a critical initialization failure.
                    throw new InvalidOperationException("Service provider is not initialized.");
                }
                var mainViewModel = _serviceProvider.GetRequiredService<MainWindowViewModel>();
                desktop.MainWindow = new MainWindow(mainViewModel);

                // 启动配置更改监听器
                _configChangeListener = _serviceProvider.GetRequiredService<ConfigurationChangeListener>();

                desktop.Exit += (s, e) =>
                {
                    _configChangeListener?.Dispose();
                    (_serviceProvider as IDisposable)?.Dispose();
                };
            }
            else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
            {
                var mainViewModel = _serviceProvider!.GetRequiredService<MainWindowViewModel>();
                singleViewPlatform.MainView = new MainWindow(mainViewModel);
            }

            base.OnFrameworkInitializationCompleted();
        }
        catch (OptionsValidationException ex)
        {
            // 处理配置验证错误
            System.Diagnostics.Debug.WriteLine($"Configuration validation failed: {string.Join(", ", ex.Failures)}");
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.Shutdown(1);
            }
            throw;
        }
    }

    private void ConfigureServices()
    {
        // 构建配置
        var builder = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables("MODULUS_")
            .AddUserSecrets<App>(optional: true);

        _configuration = builder.Build();

        var services = new ServiceCollection();

        // Add Serilog to the service collection
        services.AddLogging(loggingBuilder => loggingBuilder.AddSerilog(dispose: true));

        // 注册配置服务
        services.AddSingleton<IConfiguration>(_configuration);

        // Register IPluginService and its implementation
        services.AddSingleton<IPluginService, PluginService>();

        // 注册选项（支持热重载和验证）
        services.AddOptions<AppOptions>()
            .Bind(_configuration.GetSection(AppOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // 注册插件选项（支持热重载、验证和目录创建）
        services.AddOptions<PluginOptions>()
            .Bind(_configuration.GetSection(PluginOptions.SectionName))
            .ValidateOnStart();

        // 注册自定义选项验证器
        services.AddSingleton<IValidateOptions<PluginOptions>, PluginOptionsValidation>();

        // 注册配置更改监听器
        services.AddSingleton<ConfigurationChangeListener>();

        // 注册导航服务（单例）
        services.AddSingleton<INavigationService, NavigationService>();

        // 注册导航插件服务（单例）
        services.AddSingleton<NavigationPluginService>();

        // 注册插件管理器（单例）
        services.AddSingleton<IPluginManager, PluginManager>();

        // 注册主视图模型（单例）
        services.AddSingleton<MainWindowViewModel>();

        // 注册所有页面的ViewModel（Transient）
        services.AddTransient<DashboardViewModel>();
        services.AddTransient<PluginManagerViewModel>();
        services.AddTransient<SettingsViewModel>();

        // 创建服务提供器
        _serviceProvider = services.BuildServiceProvider();
    }
}
