using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Modulus.Core.Runtime;
using Modulus.UI.Abstractions;
using System.Collections.ObjectModel;

namespace Modulus.Host.Blazor.Shell.ViewModels;

public partial class ModuleListViewModel : ObservableObject
{
    private readonly RuntimeContext _runtimeContext;
    private readonly IModuleLoader _moduleLoader;
    private readonly IEnumerable<IModuleProvider> _moduleProviders;

    public ObservableCollection<ModuleViewModel> Modules { get; } = new();

    [ObservableProperty]
    private bool _isLoading;

    public ModuleListViewModel(
        RuntimeContext runtimeContext,
        IModuleLoader moduleLoader,
        IEnumerable<IModuleProvider> moduleProviders)
    {
        _runtimeContext = runtimeContext;
        _moduleLoader = moduleLoader;
        _moduleProviders = moduleProviders;
    }

    [RelayCommand]
    public async Task RefreshModulesAsync()
    {
        IsLoading = true;
        try
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
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task ToggleModuleAsync(ModuleViewModel moduleVm)
    {
        if (moduleVm == null) return;

        try
        {
            if (moduleVm.IsLoaded)
            {
                await _moduleLoader.UnloadAsync(moduleVm.Id);
            }
            else if (moduleVm.IsUnloaded && !string.IsNullOrEmpty(moduleVm.PackagePath))
            {
                await _moduleLoader.LoadAsync(moduleVm.PackagePath);
            }

            await RefreshModulesAsync();
        }
        catch (Exception ex)
        {
            // TODO: Show error notification
            Console.WriteLine($"Error toggling module: {ex.Message}");
        }
    }

    [RelayCommand]
    public async Task ReloadModuleAsync(ModuleViewModel moduleVm)
    {
        if (moduleVm == null) return;

        try
        {
            await _moduleLoader.ReloadAsync(moduleVm.Id);
            await RefreshModulesAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error reloading module: {ex.Message}");
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

