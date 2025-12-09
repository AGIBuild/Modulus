using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Modulus.Core.Installation;
using Modulus.Core.Runtime;
using Modulus.Infrastructure.Data.Models;
using Modulus.Infrastructure.Data.Repositories;
using Modulus.UI.Abstractions;

using DataModuleState = Modulus.Infrastructure.Data.Models.ModuleState;
using RuntimeModuleState = Modulus.Core.Runtime.ModuleState;

namespace Modulus.Host.Blazor.Shell.ViewModels;

public partial class ModuleListViewModel : ObservableObject
{
    private readonly RuntimeContext _runtimeContext;
    private readonly IModuleLoader _moduleLoader;
    private readonly IModuleRepository _moduleRepository;
    private readonly IModuleInstallerService _moduleInstaller;
    private readonly ModuleDetailLoader _detailLoader;
    private CancellationTokenSource? _detailLoadCts;

    public ObservableCollection<ModuleViewModel> Modules { get; } = new();

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _importPath = string.Empty;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private ModuleViewModel? _selectedModule;

    [ObservableProperty]
    private string _selectedModuleDetails = string.Empty;

    public ModuleListViewModel(
        RuntimeContext runtimeContext,
        IModuleLoader moduleLoader,
        IModuleRepository moduleRepository,
        IModuleInstallerService moduleInstaller,
        ILoggerFactory loggerFactory,
        IEnumerable<IModuleProvider> moduleProviders)
    {
        _runtimeContext = runtimeContext;
        _moduleLoader = moduleLoader;
        _moduleRepository = moduleRepository;
        _moduleInstaller = moduleInstaller;
        _detailLoader = new ModuleDetailLoader(loggerFactory.CreateLogger<ModuleDetailLoader>());
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
            Modules.Clear();
            var dbModules = await _moduleRepository.GetAllAsync();
            
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
        ErrorMessage = null;

        try
        {
             if (moduleVm.IsRunning)
             {
                 await _moduleLoader.UnloadAsync(moduleVm.Id);
                 await _moduleRepository.UpdateStateAsync(moduleVm.Id, DataModuleState.Disabled);
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

        try
        {
            if (moduleVm.IsRunning)
            {
                await _moduleLoader.UnloadAsync(moduleVm.Id);
            }
            
            await _moduleRepository.DeleteAsync(moduleVm.Id);
            
            // Try to clean files
            try 
            {
                var manifestPath = Path.GetFullPath(moduleVm.Entity.Path);
                var dir = Path.GetDirectoryName(manifestPath);
                if (dir != null && Directory.Exists(dir))
                {
                    Directory.Delete(dir, true);
                }
            }
            catch { /* Ignore file deletion errors */ }

            await RefreshModulesAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error removing module: {ex.Message}";
        }
    }

    [RelayCommand]
    public async Task ImportModuleAsync()
    {
        if (string.IsNullOrWhiteSpace(ImportPath)) return;
        ErrorMessage = null;

        var path = ImportPath;
        if (Directory.Exists(path))
        {
            path = Path.Combine(path, "manifest.json");
        }

        try
        {
            await _moduleInstaller.RegisterDevelopmentModuleAsync(path);
            ImportPath = string.Empty;
            await RefreshModulesAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Import failed: {ex.Message}";
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
    public string Author => Entity.Author ?? "Unknown";
    public bool IsSystem => Entity.IsSystem;
    public string MenuLocation => Entity.MenuLocation.ToString();
    
    public bool IsRunning => RuntimeModule?.State == RuntimeModuleState.Active;
    
    public string StatusText 
    {
        get 
        {
            if (Entity.State == DataModuleState.MissingFiles) return "Missing Files";
            if (Entity.State == DataModuleState.Disabled) return "Disabled";
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
}
