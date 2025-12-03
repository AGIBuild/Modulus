using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Modulus.UI.Abstractions;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Modulus.Host.Avalonia.Shell.ViewModels;

public partial class ShellViewModel : ViewModelBase
{
    private readonly IMenuRegistry _menuRegistry;
    private readonly IUIFactory _uiFactory;
    private readonly IServiceProvider _serviceProvider;
    private bool _isNavigating;

    public ObservableCollection<MenuItem> MainMenuItems { get; } = new();
    public ObservableCollection<MenuItem> BottomMenuItems { get; } = new();

    [ObservableProperty]
    private object? _currentView;

    [ObservableProperty]
    private string _currentTitle = "Modulus";

    [ObservableProperty]
    private MenuItem? _selectedMainMenuItem;

    [ObservableProperty]
    private MenuItem? _selectedBottomMenuItem;

    public ShellViewModel(
        IMenuRegistry menuRegistry, 
        IUIFactory uiFactory,
        IServiceProvider serviceProvider)
    {
        _menuRegistry = menuRegistry;
        _uiFactory = uiFactory;
        _serviceProvider = serviceProvider;
        
        RefreshMenu();
    }

    public void RefreshMenu()
    {
        MainMenuItems.Clear();
        foreach (var item in _menuRegistry.GetItems(MenuLocation.Main))
        {
            MainMenuItems.Add(item);
        }

        BottomMenuItems.Clear();
        foreach (var item in _menuRegistry.GetItems(MenuLocation.Bottom))
        {
            BottomMenuItems.Add(item);
        }
    }

    partial void OnSelectedMainMenuItemChanged(MenuItem? value)
    {
        if (value != null && !_isNavigating)
        {
            // Clear bottom selection
            _isNavigating = true;
            SelectedBottomMenuItem = null;
            _isNavigating = false;
            
            _ = NavigateToMenuItem(value);
        }
    }

    partial void OnSelectedBottomMenuItemChanged(MenuItem? value)
    {
        if (value != null && !_isNavigating)
        {
            // Clear main selection
            _isNavigating = true;
            SelectedMainMenuItem = null;
            _isNavigating = false;
            
            _ = NavigateToMenuItem(value);
        }
    }

    private async Task NavigateToMenuItem(MenuItem item)
    {
        Type? vmType = Type.GetType(item.NavigationKey);
        if (vmType == null)
        {
            vmType = AppDomain.CurrentDomain.GetAssemblies()
               .SelectMany(a => { try { return a.GetTypes(); } catch { return Array.Empty<Type>(); } })
               .FirstOrDefault(t => t.FullName == item.NavigationKey || t.Name == item.NavigationKey);
        }

        if (vmType != null)
        {
            var vm = _serviceProvider.GetService(vmType);
            if (vm == null)
            {
                vm = ActivatorUtilities.CreateInstance(_serviceProvider, vmType);
            }
            
            var view = _uiFactory.CreateView(vm);
            CurrentView = view;
            CurrentTitle = item.DisplayName;
        }
        
        await Task.CompletedTask;
    }

    /// <summary>
    /// Navigate to a specific ViewModel type directly.
    /// </summary>
    public void NavigateTo<TViewModel>() where TViewModel : class
    {
        _isNavigating = true;
        try
        {
            var vm = _serviceProvider.GetService<TViewModel>() 
                     ?? ActivatorUtilities.CreateInstance<TViewModel>(_serviceProvider);
            var view = _uiFactory.CreateView(vm);
            CurrentView = view;
            CurrentTitle = typeof(TViewModel).Name.Replace("ViewModel", "");
            
            // Find and select corresponding menu item
            var mainItem = MainMenuItems.FirstOrDefault(m => m.NavigationKey.Contains(typeof(TViewModel).Name));
            var bottomItem = BottomMenuItems.FirstOrDefault(m => m.NavigationKey.Contains(typeof(TViewModel).Name));
            
            if (mainItem != null)
            {
                SelectedMainMenuItem = mainItem;
                SelectedBottomMenuItem = null;
            }
            else if (bottomItem != null)
            {
                SelectedBottomMenuItem = bottomItem;
                SelectedMainMenuItem = null;
            }
        }
        finally
        {
            _isNavigating = false;
        }
    }
}
