using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using Modulus.Core.Installation;
using Modulus.Core.Runtime;
using Modulus.Infrastructure.Data.Models;
using Modulus.Infrastructure.Data.Repositories;
using Modulus.UI.Abstractions;
using Modulus.UI.Abstractions.Messages;

// Alias to avoid ambiguity
using DataModuleState = Modulus.Infrastructure.Data.Models.ModuleState;
using RuntimeModuleState = Modulus.Core.Runtime.ModuleState;

namespace Modulus.Host.Avalonia.Shell.ViewModels;

public partial class ModuleListViewModel : ViewModelBase
{
    private readonly RuntimeContext _runtimeContext;
    private readonly IModuleLoader _moduleLoader;
    private readonly IModuleRepository _moduleRepository;
    private readonly IMenuRepository _menuRepository;
    private readonly IMenuRegistry _menuRegistry;
    private readonly IModuleInstallerService _moduleInstaller;
    private readonly IModuleCleanupService _cleanupService;
    private readonly INotificationService? _notificationService;
    private readonly ILogger<ModuleListViewModel> _logger;
    private readonly ModuleDetailLoader _detailLoader;
    private CancellationTokenSource? _detailLoadCts;

    public ObservableCollection<ModuleViewModel> Modules { get; } = new();

    [ObservableProperty]
    private string _importPath = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FilteredModules))]
    [NotifyPropertyChangedFor(nameof(EnabledModules))]
    [NotifyPropertyChangedFor(nameof(DisabledModules))]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private ModuleViewModel? _selectedModule;

    [ObservableProperty]
    private string _selectedModuleDetails = string.Empty;

    public List<ModuleViewModel> FilteredModules => 
        (string.IsNullOrWhiteSpace(SearchText) 
            ? Modules 
            : Modules.Where(m => m.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase)))
        .ToList();

    public List<ModuleViewModel> EnabledModules => FilteredModules.Where(m => m.IsEnabled).ToList();
    public List<ModuleViewModel> DisabledModules => FilteredModules.Where(m => !m.IsEnabled).ToList();

    public ModuleListViewModel(
        RuntimeContext runtimeContext, 
        IModuleLoader moduleLoader, 
        IModuleRepository moduleRepository,
        IMenuRepository menuRepository,
        IMenuRegistry menuRegistry,
        IModuleInstallerService moduleInstaller,
        IModuleCleanupService cleanupService,
        ILoggerFactory loggerFactory,
        INotificationService? notificationService = null)
    {
        _runtimeContext = runtimeContext;
        _moduleLoader = moduleLoader;
        _moduleRepository = moduleRepository;
        _menuRepository = menuRepository;
        _menuRegistry = menuRegistry;
        _moduleInstaller = moduleInstaller;
        _cleanupService = cleanupService;
        _notificationService = notificationService;
        _logger = loggerFactory.CreateLogger<ModuleListViewModel>();
        _detailLoader = new ModuleDetailLoader(loggerFactory.CreateLogger<ModuleDetailLoader>());
        Title = "Module Management";
        
        _ = RefreshModulesAsync();
    }

    partial void OnSelectedModuleChanged(ModuleViewModel? value)
    {
        // Cancel any ongoing detail load
        _detailLoadCts?.Cancel();
        _detailLoadCts?.Dispose();
        _detailLoadCts = null;

        if (value != null)
        {
            _detailLoadCts = new CancellationTokenSource();
            _ = LoadModuleDetailsAsync(value, _detailLoadCts.Token);
        }
        else
        {
            SelectedModuleDetails = string.Empty;
        }
    }

    private async Task LoadModuleDetailsAsync(ModuleViewModel module, CancellationToken cancellationToken)
    {
        SelectedModuleDetails = "Loading...";

        var result = await _detailLoader.LoadDetailAsync(module.Entity.Path, cancellationToken);

        if (result.WasCancelled)
        {
            // User selected different module, ignore this result
            return;
        }

        SelectedModuleDetails = result.Content;

        // If detail load failed and module is in runtime, update its state
        if (!result.Success && !result.WasCancelled && module.RuntimeModule != null)
        {
            module.RuntimeModule.TransitionTo(
                RuntimeModuleState.Error,
                result.WasTimedOut ? "Detail load timed out" : "Detail load failed",
                result.Error);
        }
    }

    [RelayCommand]
    private async Task RefreshModulesAsync()
    {
        Modules.Clear();
        
        var dbModules = await _moduleRepository.GetAllAsync();
        
        foreach (var dbModule in dbModules)
        {
            // Skip built-in host modules - they shouldn't appear in the installed modules list
            if (dbModule.IsSystem && dbModule.Path == "built-in")
            {
                continue;
            }
            
            _runtimeContext.TryGetModule(dbModule.Id, out var runtimeModule);
            Modules.Add(new ModuleViewModel(dbModule, runtimeModule));
        }

        OnPropertyChanged(nameof(FilteredModules));
        OnPropertyChanged(nameof(EnabledModules));
        OnPropertyChanged(nameof(DisabledModules));
        
        if (SelectedModule == null && Modules.Any())
        {
            SelectedModule = Modules.First();
        }
        else if (SelectedModule != null)
        {
            // Reload details for currently selected module
            _detailLoadCts?.Cancel();
            _detailLoadCts?.Dispose();
            _detailLoadCts = new CancellationTokenSource();
            _ = LoadModuleDetailsAsync(SelectedModule, _detailLoadCts.Token);
        }
    }

    [RelayCommand]
    private async Task ToggleModuleAsync(ModuleViewModel moduleVm)
    {
        // System modules can be disabled but not uninstalled
        if (moduleVm == null) return;

        try
        {
            if (moduleVm.IsEnabled)
            {
                // Disable: Unload if loaded, then mark as disabled
                if (moduleVm.IsLoaded)
                {
                    await _moduleLoader.UnloadAsync(moduleVm.Id);
                }
                
                await _moduleRepository.UpdateStateAsync(moduleVm.Id, DataModuleState.Disabled);
                
                // Notify ShellViewModel to remove menus (handles both registry and UI)
                WeakReferenceMessenger.Default.Send(new MenuItemsRemovedMessage(moduleVm.Id));
            }
            else 
            {
                // Enable
                if (moduleVm.Entity.State == DataModuleState.MissingFiles)
                {
                    await (_notificationService?.ShowErrorAsync("Error", "Cannot enable module with missing files.") ?? Task.CompletedTask);
                    return;
                }

                await _moduleRepository.UpdateStateAsync(moduleVm.Id, DataModuleState.Ready);
                
                // Resolve absolute path
                var manifestPath = Path.GetFullPath(moduleVm.Entity.Path);
                var packagePath = Path.GetDirectoryName(manifestPath);
                
                if (packagePath != null)
                {
                    await _moduleLoader.LoadAsync(packagePath, moduleVm.IsSystem);
                }
                
                // Register menus from database and notify ShellViewModel (incremental)
                var addedMenus = await RegisterModuleMenusAsync(moduleVm.Id);
                if (addedMenus.Count > 0)
                {
                    WeakReferenceMessenger.Default.Send(new MenuItemsAddedMessage(addedMenus));
                }
            }
             
            await RefreshModulesAsync();
             
            OnPropertyChanged(nameof(EnabledModules));
            OnPropertyChanged(nameof(DisabledModules));
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("system module"))
        {
            // System modules cannot be unloaded while running
            await (_notificationService?.ShowErrorAsync("Cannot Disable", 
                $"System module '{moduleVm.Name}' cannot be disabled while the application is running. It is a bundled module required by the host.") ?? Task.CompletedTask);
        }
        catch (Exception ex)
        {
            await (_notificationService?.ShowErrorAsync("Error", ex.Message) ?? Task.CompletedTask);
        }
    }
    
    private async Task<List<MenuItem>> RegisterModuleMenusAsync(string moduleId)
    {
        var menus = await _menuRepository.GetByModuleIdAsync(moduleId);
        var addedItems = new List<MenuItem>();
        
        foreach (var menu in menus)
        {
            var iconKind = IconKind.Grid;
            if (Enum.TryParse<IconKind>(menu.Icon, true, out var parsedIcon))
            {
                iconKind = parsedIcon;
            }
            
            // Use NavigationKey for Avalonia (ViewModelType fullname)
            var navigationKey = menu.Route ?? menu.Id;
            
            var item = new MenuItem(
                menu.Id,
                menu.DisplayName,
                iconKind,
                navigationKey,
                menu.Location,
                menu.Order
            );
            item.ModuleId = menu.ModuleId;
            
            _menuRegistry.Register(item);
            addedItems.Add(item);
        }
        
        return addedItems;
    }

    [RelayCommand]
    private async Task RemoveModuleAsync(ModuleViewModel moduleVm)
    {
        if (moduleVm == null || moduleVm.IsSystem) return;

        try
        {
            if (moduleVm.IsLoaded)
            {
                await _moduleLoader.UnloadAsync(moduleVm.Id);
            }
            
            // Notify ShellViewModel to remove menus (handles both registry and UI)
            WeakReferenceMessenger.Default.Send(new MenuItemsRemovedMessage(moduleVm.Id));
            
            await _moduleRepository.DeleteAsync(moduleVm.Id);
            
            // Schedule cleanup via IModuleCleanupService (handles retries and persistence)
            var manifestPath = Path.GetFullPath(moduleVm.Entity.Path);
            var dir = Path.GetDirectoryName(manifestPath);
            if (dir != null && Directory.Exists(dir))
            {
                // Pass moduleId so cleanup can be cancelled if module is reinstalled
                await _cleanupService.ScheduleCleanupAsync(dir, moduleVm.Id);
                
                // Check if cleanup succeeded (directory deleted) or was scheduled for later
                if (Directory.Exists(dir))
                {
                    _logger.LogInformation("Module {ModuleId} files scheduled for cleanup on next restart.", moduleVm.Id);
                    if (_notificationService != null)
                    {
                        await _notificationService.ShowInfoAsync("Module Removed", 
                            $"Module '{moduleVm.Entity.DisplayName}' removed. Some files are locked and will be cleaned up on next restart.");
                    }
                }
            }

            await RefreshModulesAsync();
        }
        catch (Exception ex)
        {
            _notificationService?.ShowErrorAsync("Error", ex.Message);
        }
    }

    [RelayCommand]
    private async Task ImportModuleAsync()
    {
        if (string.IsNullOrWhiteSpace(ImportPath)) return;
        
        // ImportPath could be a directory or manifest.json
        var path = ImportPath;
        if (File.Exists(path) && Path.GetFileName(path) == "manifest.json")
        {
            // ok
        }
        else if (Directory.Exists(path))
        {
            path = Path.Combine(path, "manifest.json");
        }
        else
        {
            _notificationService?.ShowErrorAsync("Error", "Invalid path.");
            return;
        }

        try
        {
            await _moduleInstaller.RegisterDevelopmentModuleAsync(path);
            ImportPath = string.Empty;
            await RefreshModulesAsync();
            _notificationService?.ShowInfoAsync("Success", "Module imported.");
        }
        catch (Exception ex)
        {
            _notificationService?.ShowErrorAsync("Error", ex.Message);
        }
    }

    /// <summary>
    /// Installs a module from a .modpkg package file.
    /// Called from View after file picker selection.
    /// </summary>
    public async Task InstallPackageAsync(string packagePath)
    {
        if (string.IsNullOrWhiteSpace(packagePath)) return;

        try
        {
            // First attempt without overwrite
            var hostType = _runtimeContext.HostType;
            var result = await _moduleInstaller.InstallFromPackageAsync(packagePath, overwrite: false, hostType: hostType);

            if (result.RequiresConfirmation)
            {
                // Module exists, ask for confirmation
                var confirm = await (_notificationService?.ConfirmAsync(
                    "Module Already Exists",
                    $"Module '{result.DisplayName ?? result.ModuleId}' is already installed. Do you want to overwrite it?",
                    "Overwrite", "Cancel") ?? Task.FromResult(false));

                if (!confirm)
                {
                    return;
                }

                // Remove existing menus from UI first (regardless of module state)
                WeakReferenceMessenger.Default.Send(new MenuItemsRemovedMessage(result.ModuleId!));

                // Try to unload existing module if running
                if (_runtimeContext.TryGetModule(result.ModuleId!, out var existingModule) && 
                    existingModule?.State == RuntimeModuleState.Active)
                {
                    try
                    {
                        await _moduleLoader.UnloadAsync(result.ModuleId!);
                    }
                    catch (InvalidOperationException ex) when (ex.Message.Contains("system module"))
                    {
                        // System modules can't be unloaded, but we can still overwrite files
                        // The new version will take effect after restart
                        _logger.LogWarning("System module {ModuleId} cannot be unloaded. New version will take effect after restart.", result.ModuleId);
                    }
                }

                // Retry with overwrite
                result = await _moduleInstaller.InstallFromPackageAsync(packagePath, overwrite: true, hostType: hostType);
            }

            if (!result.Success)
            {
                await (_notificationService?.ShowErrorAsync("Installation Failed", result.Error ?? "Unknown error") ?? Task.CompletedTask);
                return;
            }

            // Auto-load the installed module
            if (result.InstallPath != null)
            {
                await _moduleLoader.LoadAsync(result.InstallPath, isSystem: false);
                
                // Register menus and notify shell
                var addedMenus = await RegisterModuleMenusAsync(result.ModuleId!);
                if (addedMenus.Count > 0)
                {
                    WeakReferenceMessenger.Default.Send(new MenuItemsAddedMessage(addedMenus));
                }
            }

            await RefreshModulesAsync();
            
            await (_notificationService?.ShowInfoAsync("Success", 
                $"Module '{result.DisplayName}' v{result.Version} installed successfully.") ?? Task.CompletedTask);
        }
        catch (Exception ex)
        {
            await (_notificationService?.ShowErrorAsync("Installation Failed", ex.Message) ?? Task.CompletedTask);
        }
    }
}

public partial class ModuleViewModel : ObservableObject
{
    public ModuleEntity Entity { get; }
    public RuntimeModule? RuntimeModule { get; }

    public ModuleViewModel(ModuleEntity entity, RuntimeModule? runtimeModule)
    {
        Entity = entity;
        RuntimeModule = runtimeModule;
    }

    public string Id => Entity.Id;
    public string Name => Entity.DisplayName;
    public string Version => Entity.Version;
    public string Description => Entity.Description ?? "No description";
    public string Author => Entity.Publisher ?? "AGIBuild";
    public bool IsSystem => Entity.IsSystem;
    public string MenuLocation => Entity.MenuLocation.ToString();
    
    /// <summary>
    /// Whether the module is enabled (based on database state, not runtime).
    /// </summary>
    public bool IsEnabled => Entity.IsEnabled && Entity.State != DataModuleState.Disabled;
    
    /// <summary>
    /// Whether the module is actually loaded and running in the runtime.
    /// </summary>
    public bool IsLoaded => RuntimeModule?.State == RuntimeModuleState.Active;
    
    // Status Logic
    public string StatusText 
    {
        get 
        {
            if (Entity.State == DataModuleState.MissingFiles) return "Missing Files";
            if (Entity.State == DataModuleState.Disabled || !Entity.IsEnabled) return "Disabled";
            if (IsLoaded) return "Running";
            return "Ready"; // Enabled but not yet loaded
        }
    }

    public string StatusColor => StatusText switch
    {
        "Running" => "#4CAF50", // Green
        "Ready" => "#2196F3", // Blue
        "Disabled" => "#9E9E9E", // Grey
        "Missing Files" => "#FFC107", // Amber/Yellow
        _ => "#9E9E9E"
    };

    /// <summary>
    /// Whether the toggle button should be shown.
    /// All modules can be toggled (disabled/enabled), including system modules.
    /// </summary>
    public bool ShowToggle => true;
    
    /// <summary>
    /// Whether this module can be removed (only non-system modules).
    /// </summary>
    public bool CanRemove => !IsSystem;
}
