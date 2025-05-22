using Microsoft.Extensions.Options;
using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;

namespace Modulus.App.Services
{
    /// <summary>
    /// 配置热重载监听器
    /// </summary>
    public class ConfigurationChangeListener : IDisposable
    {
        private readonly IOptionsMonitor<Options.AppOptions> _appOptions;
        private readonly IOptionsMonitor<Options.PluginOptions> _pluginOptions;
        private readonly IValidateOptions<Options.AppOptions> _appOptionsValidator;
        private readonly IValidateOptions<Options.PluginOptions> _pluginOptionsValidator;
        private IDisposable? _appOptionsChangeToken;
        private IDisposable? _pluginOptionsChangeToken;

        public ConfigurationChangeListener(
            IOptionsMonitor<Options.AppOptions> appOptions,
            IOptionsMonitor<Options.PluginOptions> pluginOptions,
            IValidateOptions<Options.PluginOptions> pluginOptionsValidator)
        {
            _appOptions = appOptions;
            _pluginOptions = pluginOptions;
            _pluginOptionsValidator = pluginOptionsValidator;
            
            // 初始化应用配置验证器
            _appOptionsValidator = new DataAnnotationValidateOptions<Options.AppOptions>(null);

            // 注册配置更改通知
            _appOptionsChangeToken = _appOptions.OnChange(OnAppOptionsChanged);
            _pluginOptionsChangeToken = _pluginOptions.OnChange(OnPluginOptionsChanged);
        }

        private void OnAppOptionsChanged(Options.AppOptions options, string? name)
        {
            try
            {
                var validationResult = _appOptionsValidator.Validate(name, options);
                if (!validationResult.Succeeded)
                {
                    Debug.WriteLine($"应用程序配置验证失败: {string.Join(", ", validationResult.FailureMessage)}");
                    return;
                }

                Debug.WriteLine($"应用程序配置已更改: {options.Name} v{options.Version}");
                // 在这里处理应用配置更改
                // 例如：更新UI显示、通知其他服务等
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"处理应用程序配置更改时出错: {ex.Message}");
            }
        }

        private void OnPluginOptionsChanged(Options.PluginOptions options, string? name)
        {
            try
            {
                var validationResult = _pluginOptionsValidator.Validate(name, options);
                if (!validationResult.Succeeded)
                {
                    Debug.WriteLine($"插件配置验证失败: {string.Join(", ", validationResult.FailureMessage)}");
                    return;
                }

                Debug.WriteLine($"插件配置已更改: InstallPath={options.InstallPath}, UserPath={options.UserPath}");
                // 在这里处理插件配置更改
                // 例如：重新扫描插件目录、重新加载插件等
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"处理插件配置更改时出错: {ex.Message}");
            }
        }

        public void Dispose()
        {
            _appOptionsChangeToken?.Dispose();
            _pluginOptionsChangeToken?.Dispose();
        }
    }
}
