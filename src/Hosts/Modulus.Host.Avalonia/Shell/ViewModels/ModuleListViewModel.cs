using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Modulus.Core.Installation;
using Modulus.Core.Manifest;
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
    private readonly INotificationService? _notificationService;

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
        INotificationService? notificationService = null)
    {
        _runtimeContext = runtimeContext;
        _moduleLoader = moduleLoader;
        _moduleRepository = moduleRepository;
        _menuRepository = menuRepository;
        _menuRegistry = menuRegistry;
        _moduleInstaller = moduleInstaller;
        _notificationService = notificationService;
        Title = "Module Management";
        
        _ = RefreshModulesAsync();
    }

    partial void OnSelectedModuleChanged(ModuleViewModel? value)
    {
        if (value != null)
        {
            _ = LoadModuleDetailsAsync(value);
        }
        else
        {
            SelectedModuleDetails = string.Empty;
        }
    }

    private async Task LoadModuleDetailsAsync(ModuleViewModel module)
    {
        SelectedModuleDetails = "Loading..."; 
        
        try 
        {
            var manifestPath = Path.GetFullPath(module.Entity.Path);
            var dir = Path.GetDirectoryName(manifestPath);
            
            // 1. Try README.md
            if (dir != null)
            {
                var readmePath = Path.Combine(dir, "README.md");
                if (File.Exists(readmePath))
                {
                    SelectedModuleDetails = await File.ReadAllTextAsync(readmePath);
                    return;
                }
            }

            // 2. Fallback to Manifest Description
            if (File.Exists(manifestPath))
            {
                var manifest = await ManifestReader.ReadFromFileAsync(manifestPath);
                if (!string.IsNullOrWhiteSpace(manifest?.Description))
                {
                    SelectedModuleDetails = manifest.Description;
                    return;
                }
            }
        }
        catch { /* Ignore file access errors */ }

        SelectedModuleDetails = "No description provided.";
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
            _ = LoadModuleDetailsAsync(SelectedModule);
        }
    }

    [RelayCommand]
    private async Task ToggleModuleAsync(ModuleViewModel moduleVm)
    {
        if (moduleVm == null || moduleVm.IsSystem) return;

        if (moduleVm.IsEnabled)
        {
            // Disable: Unload if loaded, then mark as disabled
            if (moduleVm.IsLoaded)
            {
                await _moduleLoader.UnloadAsync(moduleVm.Id);
            }
            
            await _moduleRepository.UpdateStateAsync(moduleVm.Id, DataModuleState.Disabled);
            
            // Unregister menus from MenuRegistry
            _menuRegistry.UnregisterModuleItems(moduleVm.Id);
            
            // Notify ShellViewModel to remove menus (incremental)
            WeakReferenceMessenger.Default.Send(new MenuItemsRemovedMessage(moduleVm.Id));
        }
        else 
        {
            // Enable
            if (moduleVm.Entity.State == DataModuleState.MissingFiles)
            {
                _notificationService?.ShowErrorAsync("Error", "Cannot enable module with missing files.");
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
            
            await _moduleRepository.DeleteAsync(moduleVm.Id);
            // Optionally clean files? For now, we just remove from DB as per task "Remove (Delete DB record + Clean folder)"
            // Clean folder logic:
            try 
            {
                var manifestPath = Path.GetFullPath(moduleVm.Entity.Path);
                var dir = Path.GetDirectoryName(manifestPath);
                if (dir != null && Directory.Exists(dir))
                {
                    // Basic safety: don't delete root or system folders
                    // TODO: Improve safety
                    Directory.Delete(dir, true);
                }
            }
            catch (Exception ex)
            {
                _notificationService?.ShowErrorAsync("Warning", $"Module removed from DB but failed to delete files: {ex.Message}");
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
            _notificationService?.ShowErrorAsync("Success", "Module imported."); // Using ShowError as ShowInfo might not exist
        }
        catch (Exception ex)
        {
            _notificationService?.ShowErrorAsync("Error", ex.Message);
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
    public string Name => Entity.Name;
    public string Version => Entity.Version;
    public string Description => Entity.Description ?? "No description";
    public string Author => Entity.Author ?? "AGIBuild";
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
    /// Whether the toggle button should be shown (non-system modules can be toggled).
    /// </summary>
    public bool ShowToggle => !IsSystem;
    
    /// <summary>
    /// Whether this module can be removed (only non-system modules).
    /// </summary>
    public bool CanRemove => !IsSystem;
}
