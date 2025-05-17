using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace Modulus.App.ViewModels;

/// <summary>
/// ViewModel for an individual plugin in the Plugin Manager.
/// </summary>
public partial class PluginViewModel : ObservableObject
{
    [ObservableProperty] private string pluginName = string.Empty;
    [ObservableProperty] private string pluginVersion = string.Empty;
    [ObservableProperty] private string pluginAuthor = string.Empty;
    [ObservableProperty] private string pluginDescription = string.Empty;
    [ObservableProperty] private string[] pluginTags = Array.Empty<string>();
    [ObservableProperty] private bool pluginIsEnabled = true;
    [ObservableProperty] private bool pluginIsSelected = false;
    [ObservableProperty] private string pluginStatus = "Enabled";
    [ObservableProperty] private bool pluginHasUpdate = false;
    [ObservableProperty] private string pluginUsage = string.Empty;
}
