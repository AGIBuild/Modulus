using System;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Modulus.Modules.$ext_safeprojectname$.ViewModels;

namespace Modulus.Modules.$ext_safeprojectname$.UI.Avalonia;

public partial class MainView : UserControl
{
    public MainView() : this(Design.IsDesignMode ? new MainViewModel() : throw new InvalidOperationException("MainView must be created via DI."))
    {
    }

    public MainView(MainViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}

