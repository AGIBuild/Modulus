using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace Modulus.App.ViewModels;

/// <summary>
/// ViewModel for an individual plugin in the Plugin Manager.
/// </summary>
public partial class PluginViewModel : ObservableObject
{
    /// <summary>
    /// 插件名称
    /// </summary>
    [ObservableProperty] private string pluginName = string.Empty;
    
    /// <summary>
    /// 插件版本
    /// </summary>
    [ObservableProperty] private string pluginVersion = "1.0.0";
    
    /// <summary>
    /// 插件作者
    /// </summary>
    [ObservableProperty] private string pluginAuthor = string.Empty;
    
    /// <summary>
    /// 插件描述
    /// </summary>
    [ObservableProperty] private string pluginDescription = string.Empty;
    
    /// <summary>
    /// 插件是否启用
    /// </summary>
    [ObservableProperty] private bool pluginIsEnabled;
    
    /// <summary>
    /// 插件状态
    /// </summary>
    [ObservableProperty] private string pluginStatus = "Unknown";

    [ObservableProperty] private string[] pluginTags = Array.Empty<string>();
    [ObservableProperty] private bool pluginIsSelected = false;
    [ObservableProperty] private bool pluginHasUpdate = false;
    [ObservableProperty] private string pluginUsage = string.Empty;
}
