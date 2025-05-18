using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Modulus.Plugin.Abstractions
{
    /// <summary>
    /// ILocalizer的基础实现，用于加载和管理多语言资源。
    /// </summary>
    public class PluginLocalizer : ILocalizer
    {
        private readonly Dictionary<string, Dictionary<string, string>> _resources = new();
        private readonly string _pluginDirectory;
        private readonly ILogger<PluginLocalizer>? _logger;
        private string _currentLanguage = "en"; // 默认语言为英文

        /// <summary>
        /// 创建一个新的插件本地化器实例。
        /// </summary>
        /// <param name="pluginDirectory">插件所在目录路径，用于查找语言文件。</param>
        /// <param name="logger">可选的日志记录器。</param>
        public PluginLocalizer(string pluginDirectory, ILogger<PluginLocalizer>? logger = null)
        {
            _pluginDirectory = pluginDirectory ?? throw new ArgumentNullException(nameof(pluginDirectory));
            _logger = logger;
            LoadLanguageResources();
        }

        /// <summary>
        /// 通过键获取当前语言的本地化字符串。
        /// </summary>
        /// <param name="key">资源键。</param>
        /// <returns>本地化字符串，如果找不到则返回键本身。</returns>
        public string this[string key]
        {
            get
            {
                if (_resources.TryGetValue(_currentLanguage, out var langResources) &&
                    langResources.TryGetValue(key, out var value))
                {
                    return value;
                }

                // 如果当前语言没有找到，尝试从默认语言（英文）获取
                if (_currentLanguage != "en" && 
                    _resources.TryGetValue("en", out var defaultResources) &&
                    defaultResources.TryGetValue(key, out var defaultValue))
                {
                    return defaultValue;
                }

                _logger?.LogWarning("Resource key '{Key}' not found for language '{Language}'", key, _currentLanguage);
                return key; // 返回键本身作为后备
            }
        }

        /// <summary>
        /// 获取当前语言代码。
        /// </summary>
        public string CurrentLanguage => _currentLanguage;

        /// <summary>
        /// 设置当前语言。
        /// </summary>
        /// <param name="lang">要切换到的语言代码。</param>
        public void SetLanguage(string lang)
        {
            if (string.IsNullOrEmpty(lang))
            {
                throw new ArgumentNullException(nameof(lang));
            }

            if (!_resources.ContainsKey(lang))
            {
                _logger?.LogWarning("Language '{Language}' not supported. Available languages: {Languages}", 
                    lang, string.Join(", ", _resources.Keys));
                return;
            }

            _currentLanguage = lang;
            _logger?.LogInformation("Switched to language: {Language}", lang);
        }

        /// <summary>
        /// 获取所有支持的语言代码。
        /// </summary>
        public IEnumerable<string> SupportedLanguages => _resources.Keys;

        /// <summary>
        /// 加载插件目录中的所有语言资源文件。
        /// </summary>
        private void LoadLanguageResources()
        {
            try
            {
                var langFiles = Directory.GetFiles(_pluginDirectory, "lang.*.json");
                
                if (langFiles.Length == 0)
                {
                    _logger?.LogWarning("No language files found in plugin directory: {Directory}", _pluginDirectory);
                    // 确保至少有一个空的英文资源集
                    _resources["en"] = new Dictionary<string, string>();
                    return;
                }

                foreach (var langFile in langFiles)
                {
                    // 从文件名中提取语言代码，例如从 "lang.en.json" 提取 "en"
                    var fileName = Path.GetFileName(langFile);
                    var langCode = fileName.Substring(5, fileName.Length - 10); // 去掉 "lang." 和 ".json"

                    try
                    {
                        var json = File.ReadAllText(langFile);
                        var resources = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                        
                        if (resources != null)
                        {
                            _resources[langCode] = resources;
                            _logger?.LogInformation("Loaded language file: {File} with {Count} resources", 
                                fileName, resources.Count);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "Failed to load language file: {File}", langFile);
                    }
                }

                // 如果没有加载到任何资源，或者没有英文资源，添加一个空的英文资源集
                if (_resources.Count == 0 || !_resources.ContainsKey("en"))
                {
                    _resources["en"] = new Dictionary<string, string>();
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to load language resources from directory: {Directory}", _pluginDirectory);
                // 确保至少有一个空的英文资源集
                _resources["en"] = new Dictionary<string, string>();
            }
        }
    }
}
