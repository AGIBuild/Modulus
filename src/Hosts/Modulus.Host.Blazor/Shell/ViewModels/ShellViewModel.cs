using CommunityToolkit.Mvvm.ComponentModel;
using Modulus.UI.Abstractions;
using System.Collections.ObjectModel;
using UiMenuItem = Modulus.UI.Abstractions.MenuItem;

namespace Modulus.Host.Blazor.Shell.ViewModels;

public partial class ShellViewModel : ObservableObject
{
    private readonly IMenuRegistry _menuRegistry;

    public ObservableCollection<UiMenuItem> MainMenuItems { get; } = new();
    public ObservableCollection<UiMenuItem> BottomMenuItems { get; } = new();

    [ObservableProperty]
    private string _currentTitle = "Modulus";

    [ObservableProperty]
    private UiMenuItem? _selectedMenuItem;

    public ShellViewModel(IMenuRegistry menuRegistry)
    {
        _menuRegistry = menuRegistry;
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

    public void SelectMenuItem(UiMenuItem item)
    {
        SelectedMenuItem = item;
        CurrentTitle = item.DisplayName;
    }
}
