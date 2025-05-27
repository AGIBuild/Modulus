using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Modulus.App.Services;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using Modulus.Plugin.Abstractions;
using Avalonia.Platform.Storage;
using Avalonia.Controls;

namespace Modulus.App.ViewModels
{
    /// <summary>
    /// ViewModel for the Plugin Manager view.
    /// </summary>
    public partial class PluginManagerViewModel : NavigationViewModelBase
    {
        private readonly IPluginManager _pluginManager;
        private readonly IPluginService _pluginService;

        /// <summary>
        /// 视图名称
        /// </summary>
        public override string ViewName => "PluginManagerView";

        /// <summary>
        /// 插件列表
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<PluginItemViewModel> _plugins = new(); // Renamed from plugins to follow convention

        /// <summary>
        /// 选中的插件
        /// </summary>
        [ObservableProperty]
        private PluginItemViewModel? _selectedPlugin; // Renamed from selectedPlugin

        /// <summary>
        /// 搜索文本
        /// </summary>
        [ObservableProperty]
        private string _searchText = string.Empty; // Renamed from searchText

        /// <summary>
        /// 加载状态
        /// </summary>
        [ObservableProperty]
        private bool _isLoading;

        /// <summary>
        /// 状态消息
        /// </summary>
        [ObservableProperty]
        private string _statusMessage = string.Empty;
        /// 创建插件管理器视图模型
        /// </summary>
        public PluginManagerViewModel(INavigationService navigationService, IPluginManager pluginManager, IPluginService pluginService) // Added IPluginService
            : base(navigationService)
        {
            _pluginManager = pluginManager ?? throw new ArgumentNullException(nameof(pluginManager));
            _pluginService = pluginService ?? throw new ArgumentNullException(nameof(pluginService)); // Added IPluginService initialization
            
            // 初始化插件列表
            RefreshPluginsList();
        }

        public override void OnNavigatedTo(object? parameter)
        {
            base.OnNavigatedTo(parameter);
            RefreshPluginsList(); // Refresh the list when navigated to this page
        }

        private void RefreshPluginsList()
        {
            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] PluginManagerViewModel: RefreshPluginsList called.");
            Plugins.Clear(); 

            if (_pluginManager?.LoadedPlugins == null)
            {
                System.Diagnostics.Debug.WriteLine("PluginManagerViewModel: _pluginManager or _pluginManager.LoadedPlugins is null.");
                StatusMessage = "Error: Plugin manager not available.";
                return;
            }

            System.Diagnostics.Debug.WriteLine($"PluginManagerViewModel: Found {_pluginManager.LoadedPlugins.Count} loaded plugins in PluginManager.");

            foreach (var plugin in _pluginManager.LoadedPlugins)
            {
                var meta = plugin.GetMetadata();
                if (meta == null) 
                {
                    System.Diagnostics.Debug.WriteLine("PluginManagerViewModel: Plugin with null metadata found.");
                    continue;
                }
                System.Diagnostics.Debug.WriteLine($"PluginManagerViewModel: Processing plugin - {meta.Name}");
                var isEnabled = _pluginService.IsPluginEnabled(meta.Name);
                var viewModel = new PluginItemViewModel(meta, isEnabled, _pluginService);
                Plugins.Add(viewModel);
            }
            StatusMessage = $"Plugin list refreshed. Displaying {Plugins.Count} plugins.";
            System.Diagnostics.Debug.WriteLine($"PluginManagerViewModel: Plugins collection now has {Plugins.Count} items.");
        }

        /// <summary>
        /// 从 DLL 安装插件命令
        /// </summary>
        [RelayCommand]
        private async Task InstallPluginAsync()
        {
            var topLevel = TopLevel.GetTopLevel(null);
            if (topLevel == null)
            {
                StatusMessage = "无法打开文件对话框。TopLevel 不可用。";
                return;
            }

            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "选择插件 DLL",
                AllowMultiple = false,
                FileTypeFilter = new[] { new FilePickerFileType("插件 DLLs") { Patterns = new[] { "*.dll" } } }
            });

            if (files.Count >= 1)
            {
                var filePath = files[0].TryGetLocalPath(); // 获取本地路径
                if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                {
                    StatusMessage = "请选择有效的插件文件 (.dll)。";
                    return;
                }

                IsLoading = true;
                StatusMessage = $"正在安装插件 {Path.GetFileName(filePath)}...";
                try
                {
                    await _pluginService.InstallPluginAsync(filePath);
                    RefreshPluginsList(); 
                    StatusMessage = "插件安装成功。您可能需要重新启动或全局重新加载插件。";
                }
                catch (Exception ex)
                {
                    StatusMessage = $"插件安装失败: {ex.Message}";
                }
                finally
                {
                    IsLoading = false;
                }
            }
            else
            {
                StatusMessage = "插件安装已取消。";
            }
        }

        /// <summary>
        /// 从文件夹安装插件命令
        /// </summary>
        [RelayCommand]
        private async Task InstallFromFolderAsync()
        {
            var topLevel = TopLevel.GetTopLevel(null);
            if (topLevel == null)
            {
                StatusMessage = "无法打开文件夹对话框。TopLevel 不可用。";
                return;
            }

            var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "选择插件文件夹",
                AllowMultiple = false
            });

            if (folders.Count >= 1)
            {
                var folderPath = folders[0].TryGetLocalPath(); // 获取本地路径
                if (string.IsNullOrEmpty(folderPath) || !Directory.Exists(folderPath))
                {
                    StatusMessage = "请选择有效的插件文件夹。";
                    return;
                }

                IsLoading = true;
                StatusMessage = $"正在从文件夹安装插件 {Path.GetFileName(folderPath)}...";
                try
                {
                    // Look for plugin dlls in the folder
                    var dlls = Directory.GetFiles(folderPath, "*.dll");
                    if (dlls.Length == 0)
                    {
                        StatusMessage = "在所选文件夹中未找到插件 DLL。";
                        return;
                    }

                    foreach (var dll in dlls)
                    {
                        await _pluginService.InstallPluginAsync(dll);
                    }

                    RefreshPluginsList(); 
                    StatusMessage = "插件从文件夹安装成功。您可能需要重新启动或全局重新加载插件。";
                }
                catch (Exception ex)
                {
                    StatusMessage = $"从文件夹安装插件失败: {ex.Message}";
                }
                finally
                {
                    IsLoading = false;
                }
            }
            else
            {
                StatusMessage = "插件安装已取消。";
            }
        }

        /// <summary>
        /// 从 VSIX 包安装插件命令
        /// </summary>
        [RelayCommand]
        private async Task InstallFromVsixAsync()
        {
            var topLevel = TopLevel.GetTopLevel(null);
            if (topLevel == null)
            {
                StatusMessage = "无法打开文件对话框。TopLevel 不可用。";
                return;
            }

            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "选择插件包",
                AllowMultiple = false,
                FileTypeFilter = new[] { 
                    new FilePickerFileType("插件包") { 
                        Patterns = new[] { "*.vsix", "*.zip" } 
                    } 
                }
            });

            if (files.Count >= 1)
            {
                var filePath = files[0].TryGetLocalPath(); // 获取本地路径
                if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                {
                    StatusMessage = "请选择有效的插件包 (.vsix 或 .zip)。";
                    return;
                }

                IsLoading = true;
                StatusMessage = $"正在从包安装插件 {Path.GetFileName(filePath)}...";
                try
                {
                    // Extract the package to a temporary directory
                    var tempDir = Path.Combine(Path.GetTempPath(), "ModulusPlugins", Path.GetFileNameWithoutExtension(filePath));
                    Directory.CreateDirectory(tempDir);

                    System.IO.Compression.ZipFile.ExtractToDirectory(filePath, tempDir, true);

                    // Look for plugin dlls in the extracted folder
                    var dlls = Directory.GetFiles(tempDir, "*.dll", SearchOption.AllDirectories);
                    if (dlls.Length == 0)
                    {
                        StatusMessage = "在插件包中未找到插件 DLL。";
                        return;
                    }

                    foreach (var dll in dlls)
                    {
                        await _pluginService.InstallPluginAsync(dll);
                    }

                    RefreshPluginsList(); 
                    StatusMessage = "插件包安装成功。您可能需要重新启动或全局重新加载插件。";

                    // Cleanup temporary directory
                    try
                    {
                        Directory.Delete(tempDir, true);
                    }
                    catch
                    {
                        // Ignore cleanup errors
                    }
                }
                catch (Exception ex)
                {
                    StatusMessage = $"安装插件包失败: {ex.Message}";
                }
                finally
                {
                    IsLoading = false;
                }
            }
            else
            {
                StatusMessage = "插件安装已取消。";
            }
        }

        /// <summary>
        /// 刷新插件列表命令
        /// </summary>
        [RelayCommand]
        private void RefreshPluginListCommand()
        {
            RefreshPluginsList();
        }

        /// <summary>
        /// 卸载插件命令
        /// </summary>
        [RelayCommand]
        private async Task UninstallSelectedPluginAsync()
        {
            if (SelectedPlugin == null)
            {
                StatusMessage = "请选择要卸载的插件。";
                return;
            }

            IsLoading = true;
            var pluginNameToUninstall = SelectedPlugin.Name;
            StatusMessage = $"正在卸载插件 {pluginNameToUninstall}...";
            try
            {
                await _pluginService.UninstallPluginAsync(pluginNameToUninstall);
                RefreshPluginsList(); // Refresh the list after uninstallation
                StatusMessage = $"插件 {pluginNameToUninstall} 卸载成功。您可能需要重新启动或全局重新加载插件。";
            }
            catch (Exception ex)
            {
                StatusMessage = $"插件 {pluginNameToUninstall} 卸载失败: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 启用/禁用插件命令
        /// </summary>
        [RelayCommand]
        private async Task ToggleEnableDisableSelectedPluginAsync()
        {
            if (SelectedPlugin == null)
            {
                StatusMessage = "请选择要启用或禁用的插件。";
                return;
            }

            IsLoading = true;
            var pluginToToggle = SelectedPlugin;
            string action = pluginToToggle.IsEnabled ? "禁用" : "启用";
            StatusMessage = $"正在{action}插件 {pluginToToggle.Name}...";

            try
            {
                if (pluginToToggle.IsEnabled)
                {
                    await _pluginService.DisablePluginAsync(pluginToToggle.Name);
                }
                else
                {
                    await _pluginService.EnablePluginAsync(pluginToToggle.Name);
                }
                RefreshPluginsList(); 
                StatusMessage = $"插件 {pluginToToggle.Name} 已成功{(pluginToToggle.IsEnabled ? "禁用" : "启用")}。";
            }
            catch (Exception ex)
            {
                StatusMessage = $"插件 {pluginToToggle.Name} {action}失败: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}
