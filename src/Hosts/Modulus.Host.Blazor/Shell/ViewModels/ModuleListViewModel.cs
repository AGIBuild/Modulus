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

using DataModuleState = Modulus.Infrastructure.Data.Models.ModuleState;
using RuntimeModuleState = Modulus.Core.Runtime.ModuleState;
using UiMenuItem = Modulus.UI.Abstractions.MenuItem;

namespace Modulus.Host.Blazor.Shell.ViewModels;

public partial class ModuleListViewModel : ViewModelBase
{
    private readonly RuntimeContext _runtimeContext;
    private readonly IModuleLoader _moduleLoader;
    private readonly IModuleRepository _moduleRepository;
    private readonly IMenuRepository _menuRepository;
    private readonly IMenuRegistry _menuRegistry;
    private readonly IModuleInstallerService _moduleInstaller;
    private readonly IModuleCleanupService _cleanupService;
    private readonly ILogger<ModuleListViewModel> _logger;
    private readonly ModuleDetailLoader _detailLoader;
    private CancellationTokenSource? _detailLoadCts;

    public ObservableCollection<ModuleViewModel> Modules { get; } = new();

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private string? _successMessage;

    [ObservableProperty]
    private bool _showOverwriteConfirm;

    [ObservableProperty]
    private string? _pendingPackagePath;

    [ObservableProperty]
    private string? _pendingModuleId;

    [ObservableProperty]
    private string? _pendingModuleName;

    [ObservableProperty]
    private ModuleViewModel? _selectedModule;

    [ObservableProperty]
    private string _selectedModuleDetails = string.Empty;

    public ModuleListViewModel(
        RuntimeContext runtimeContext,
        IModuleLoader moduleLoader,
        IModuleRepository moduleRepository,
        IMenuRepository menuRepository,
        IMenuRegistry menuRegistry,
        IModuleInstallerService moduleInstaller,
        IModuleCleanupService cleanupService,
        ILoggerFactory loggerFactory)
    {
        _runtimeContext = runtimeContext;
        _moduleLoader = moduleLoader;
        _moduleRepository = moduleRepository;
        _menuRepository = menuRepository;
        _menuRegistry = menuRegistry;
        _moduleInstaller = moduleInstaller;
        _cleanupService = cleanupService;
        _logger = loggerFactory.CreateLogger<ModuleListViewModel>();
        _detailLoader = new ModuleDetailLoader(loggerFactory.CreateLogger<ModuleDetailLoader>());
        Title = "Module Management";
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
    public async Task RefreshModulesAsync()
    {
        IsLoading = true;
        ErrorMessage = null;
        try
        {
            foreach (var vm in Modules)
            {
                if (vm is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
            Modules.Clear();
            var dbModules = await _moduleRepository.GetAllAsync();
            // Hide the host pseudo-module from Installed Modules list.
            // Host is still stored in DB for menu projection but should not appear as an installed module.
            dbModules = dbModules
                .Where(m => !string.Equals(m.Path, "built-in", StringComparison.OrdinalIgnoreCase))
                .ToList();
            
            foreach (var dbModule in dbModules)
            {
                 var isLoaded = _runtimeContext.TryGetModule(dbModule.Id, out var runtimeModule);
                 Modules.Add(new ModuleViewModel(dbModule, runtimeModule));
            }
            
            if (SelectedModule == null && Modules.Any())
            {
                SelectedModule = Modules.First();
            }
            else if (SelectedModule != null)
            {
                // Check if selected module still exists or needs refresh
                var existing = Modules.FirstOrDefault(m => m.Id == SelectedModule.Id);
                if (existing != null)
                {
                    SelectedModule = existing; // Re-select to update UI state
                }
                else
                {
                    SelectedModule = Modules.FirstOrDefault();
                }
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load modules: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task ToggleModuleAsync(ModuleViewModel moduleVm)
    {
        if (moduleVm == null) return;
        if (moduleVm.IsSystem)
        {
            ErrorMessage = $"Built-in module '{moduleVm.Name}' cannot be enabled/disabled.";
            return;
        }
        ErrorMessage = null;

        try
        {
             if (moduleVm.IsRunning)
             {
                 await _moduleLoader.UnloadAsync(moduleVm.Id);
                 await _moduleRepository.UpdateStateAsync(moduleVm.Id, DataModuleState.Disabled);
                 
                 // Notify ShellViewModel to remove menus (incremental)
                 WeakReferenceMessenger.Default.Send(new MenuItemsRemovedMessage(moduleVm.Id));
             }
             else
             {
                 if (moduleVm.Entity.State == DataModuleState.MissingFiles)
                 {
                     ErrorMessage = "Cannot enable module with missing files.";
                     return;
                 }

                 await _moduleRepository.UpdateStateAsync(moduleVm.Id, DataModuleState.Ready);
                 
                 var manifestPath = Path.GetFullPath(moduleVm.Entity.Path);
                 var packagePath = Path.GetDirectoryName(manifestPath);
                 
                 if (packagePath != null)
                 {
                     await _moduleLoader.LoadAsync(packagePath, moduleVm.IsSystem);

                     // Register menus from database and notify shell (incremental)
                     var addedMenus = await RegisterModuleMenusAsync(moduleVm.Id);
                     if (addedMenus.Count > 0)
                     {
                         WeakReferenceMessenger.Default.Send(new MenuItemsAddedMessage(addedMenus));
                     }
                 }
             }

            await RefreshModulesAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error toggling module: {ex.Message}";
        }
    }

    [RelayCommand]
    public async Task RemoveModuleAsync(ModuleViewModel moduleVm)
    {
        if (moduleVm == null || moduleVm.IsSystem) return;
        ErrorMessage = null;
        SuccessMessage = null;

        try
        {
            if (moduleVm.IsRunning)
            {
                await _moduleLoader.UnloadAsync(moduleVm.Id);
            }
            
            // Notify ShellViewModel to remove menus
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
                    SuccessMessage = $"Module '{moduleVm.Name}' removed. Some files are locked and will be cleaned up on next restart.";
                }
                else
                {
                    SuccessMessage = $"Module '{moduleVm.Name}' removed successfully.";
                }
            }
            else
            {
                SuccessMessage = $"Module '{moduleVm.Name}' removed successfully.";
            }

            await RefreshModulesAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error removing module: {ex.Message}";
        }
    }

    /// <summary>
    /// Installs a module from a .modpkg package stream (for file upload).
    /// </summary>
    public async Task InstallFromStreamAsync(Stream packageStream, string fileName, bool overwrite = false)
    {
        ErrorMessage = null;
        SuccessMessage = null;
        IsLoading = true;

        try
        {
            var hostType = _runtimeContext.HostType;
            var result = await _moduleInstaller.InstallFromPackageStreamAsync(packageStream, fileName, overwrite, hostType: hostType);

            if (result.RequiresConfirmation)
            {
                // Store for confirmation dialog
                PendingPackagePath = null; // We'll need to re-upload for stream
                PendingModuleId = result.ModuleId;
                PendingModuleName = result.DisplayName ?? result.ModuleId;
                ShowOverwriteConfirm = true;
                IsLoading = false;
                return;
            }

            if (!result.Success)
            {
                ErrorMessage =
                    result.Error ??
                    "Installation failed. The selected package may not be compatible with this host.";
                IsLoading = false;
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
            SuccessMessage = $"Module '{result.DisplayName}' v{result.Version} installed successfully.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Installation failed: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Confirms overwrite of an existing module (called from UI after confirmation dialog).
    /// </summary>
    public async Task ConfirmOverwriteAsync(Stream packageStream, string fileName)
    {
        ShowOverwriteConfirm = false;
        
        // Unload existing module if running
        if (PendingModuleId != null && 
            _runtimeContext.TryGetModule(PendingModuleId, out var existingModule) && 
            existingModule?.State == RuntimeModuleState.Active)
        {
            await _moduleLoader.UnloadAsync(PendingModuleId);
            WeakReferenceMessenger.Default.Send(new MenuItemsRemovedMessage(PendingModuleId));
        }

        PendingModuleId = null;
        PendingModuleName = null;
        
        await InstallFromStreamAsync(packageStream, fileName, overwrite: true);
    }

    /// <summary>
    /// Cancels the overwrite confirmation.
    /// </summary>
    public void CancelOverwrite()
    {
        ShowOverwriteConfirm = false;
        PendingModuleId = null;
        PendingModuleName = null;
        PendingPackagePath = null;
    }

    private async Task<List<UiMenuItem>> RegisterModuleMenusAsync(string moduleId)
    {
        var menus = await _menuRepository.GetByModuleIdAsync(moduleId);
        var addedItems = new List<UiMenuItem>();
        
        foreach (var menu in menus)
        {
            var iconKind = IconKind.Grid;
            if (Enum.TryParse<IconKind>(menu.Icon, true, out var parsedIcon))
            {
                iconKind = parsedIcon;
            }
            
            var navigationKey = menu.Route ?? menu.Id;
            
            var item = new UiMenuItem(
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
}

public partial class ModuleViewModel : ViewModelBase, IDisposable
{
    public ModuleEntity Entity { get; }
    public RuntimeModule? RuntimeModule { get; }

    public ModuleViewModel(ModuleEntity entity, RuntimeModule? runtimeModule)
    {
        Entity = entity;
        RuntimeModule = runtimeModule;
        Title = Entity.DisplayName;
        if (RuntimeModule != null)
        {
            RuntimeModule.StateChanged += OnRuntimeModuleStateChanged;
        }
    }

    public string Id => Entity.Id;
    public string Name => Entity.DisplayName;
    public string Version => Entity.Version;
    public string Author => Entity.Publisher ?? "Unknown";
    public bool IsSystem => Entity.IsSystem;
    public string MenuLocation => Entity.MenuLocation.ToString();
    
    public bool IsRunning => RuntimeModule?.State is RuntimeModuleState.Loaded or RuntimeModuleState.Active;
    
    public string StatusText 
    {
        get 
        {
            if (Entity.State == DataModuleState.MissingFiles) return "Missing Files";
            if (Entity.State == DataModuleState.Disabled) return "Disabled";
            if (RuntimeModule?.State == RuntimeModuleState.Error) return "Error";
            if (IsRunning) return "Running";
            return "Stopped";
        }
    }

    public string StatusColor => StatusText switch
    {
        "Running" => "success",
        "Disabled" => "secondary",
        "Missing Files" => "warning",
        _ => "primary"
    };

    public bool ShowToggle => !IsSystem; 
    public bool CanRemove => !IsSystem;

    private void OnRuntimeModuleStateChanged(object? sender, RuntimeModuleStateChangedEventArgs e)
    {
        OnPropertyChanged(nameof(IsRunning));
        OnPropertyChanged(nameof(StatusText));
        OnPropertyChanged(nameof(StatusColor));
    }

    public void Dispose()
    {
        if (RuntimeModule != null)
        {
            RuntimeModule.StateChanged -= OnRuntimeModuleStateChanged;
        }
    }
}
