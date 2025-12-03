using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Modulus.Core.Runtime;
using Modulus.UI.Abstractions;

namespace Modulus.Host.Avalonia.Shell.ViewModels;

public partial class ModuleListViewModel : ViewModelBase
{
    private readonly RuntimeContext _runtimeContext;
    private readonly IModuleLoader _moduleLoader;
    private readonly IEnumerable<IModuleProvider> _moduleProviders;
    private readonly INotificationService? _notificationService;

    public ObservableCollection<ModuleViewModel> Modules { get; } = new();

    public ModuleListViewModel(
        RuntimeContext runtimeContext, 
        IModuleLoader moduleLoader, 
        IEnumerable<IModuleProvider> moduleProviders,
        INotificationService? notificationService = null)
    {
        _runtimeContext = runtimeContext;
        _moduleLoader = moduleLoader;
        _moduleProviders = moduleProviders;
        _notificationService = notificationService;
        Title = "Module Management";
        
        _ = RefreshModulesAsync();
    }

    [RelayCommand]
    private async Task RefreshModulesAsync()
    {
        Modules.Clear();
        
        var loadedModules = _runtimeContext.RuntimeModules.ToDictionary(m => m.Descriptor.Id);

        foreach (var provider in _moduleProviders)
        {
            var paths = await provider.GetModulePackagesAsync();
            foreach (var path in paths)
            {
                var descriptor = await _moduleLoader.GetDescriptorAsync(path);
                if (descriptor == null) continue;

                if (loadedModules.TryGetValue(descriptor.Id, out var loadedModule))
                {
                    Modules.Add(new ModuleViewModel(loadedModule));
                    loadedModules.Remove(descriptor.Id);
                }
                else
                {
                    Modules.Add(new ModuleViewModel(descriptor, path, ModuleState.Unloaded));
                }
            }
        }

        foreach (var remaining in loadedModules.Values)
        {
             Modules.Add(new ModuleViewModel(remaining));
        }
    }

    [RelayCommand]
    private async Task ToggleModuleAsync(ModuleViewModel moduleVm)
    {
        if (moduleVm == null) return;

        try 
        {
             if (moduleVm.State == ModuleState.Active || moduleVm.State == ModuleState.Loaded)
             {
                 await _moduleLoader.UnloadAsync(moduleVm.Id);
             }
             else if (moduleVm.State == ModuleState.Unloaded) 
             {
                 if (string.IsNullOrEmpty(moduleVm.PackagePath))
                 {
                     _notificationService?.ShowErrorAsync("Error", "Cannot load module: package path unknown.");
                     return;
                 }
                 await _moduleLoader.LoadAsync(moduleVm.PackagePath);
             }
             
             await RefreshModulesAsync();
        }
        catch (Exception ex)
        {
            _notificationService?.ShowErrorAsync("Error", ex.Message);
        }
    }

    [RelayCommand]
    private async Task ReloadModuleAsync(ModuleViewModel moduleVm)
    {
         if (moduleVm == null) return;
         
         try
         {
             await _moduleLoader.ReloadAsync(moduleVm.Id);
             await RefreshModulesAsync();
         }
         catch (Exception ex)
         {
             _notificationService?.ShowErrorAsync("Error", ex.Message);
         }
    }
}

public partial class ModuleViewModel : ObservableObject
{
    private readonly RuntimeModule? _runtimeModule;
    private readonly ModuleDescriptor _descriptor;
    private readonly string _packagePath;
    
    [ObservableProperty]
    private ModuleState _state;

    public string Id => _descriptor.Id;
    public string DisplayName => _descriptor.DisplayName;
    public string Description => _descriptor.Description;
    public string Version => _descriptor.Version;
    public string PackagePath => _packagePath;
    public bool IsSystem => _runtimeModule?.IsSystem ?? false;
    
    public string StatusColor => State switch
    {
        ModuleState.Active => "#4CAF50",
        ModuleState.Loaded => "#2196F3",
        ModuleState.Error => "#F44336",
        _ => "#9E9E9E"
    };

    public ModuleViewModel(RuntimeModule module)
    {
        _runtimeModule = module;
        _descriptor = module.Descriptor;
        _packagePath = module.PackagePath;
        State = module.State;
    }

    public ModuleViewModel(ModuleDescriptor descriptor, string packagePath, ModuleState state)
    {
        _runtimeModule = null;
        _descriptor = descriptor;
        _packagePath = packagePath;
        State = state;
    }

    public bool IsLoaded => State == ModuleState.Active || State == ModuleState.Loaded || State == ModuleState.Error;
    public bool IsUnloaded => State == ModuleState.Unloaded;
    public bool CanUnload => IsLoaded && !IsSystem;
}

