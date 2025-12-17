using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Modulus.Modules.Home.ViewModels;

namespace Modulus.Modules.Home.UI.Avalonia;

public partial class HomeView : UserControl
{
    public HomeView()
    {
        InitializeComponent();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        
        if (DataContext is HomeViewModel vm)
        {
            _ = vm.LoadCommand.ExecuteAsync(null);
        }
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        
        // Trigger entry animations with staggered delays
        TriggerEntryAnimations();
    }

    private void TriggerEntryAnimations()
    {
        // Staggered entry animations for each section
        var headerSection = this.FindControl<Grid>("HeaderSection");
        var heroSection = this.FindControl<Grid>("HeroSection");
        var featuresSection = this.FindControl<StackPanel>("FeaturesSection");
        var cliSection = this.FindControl<StackPanel>("CliSection");

        // Header section: immediate
        Dispatcher.UIThread.Post(() =>
        {
            headerSection?.Classes.Add("loaded");
        }, DispatcherPriority.Loaded);

        // Hero section: 100ms delay
        Dispatcher.UIThread.Post(async () =>
        {
            await Task.Delay(100);
            heroSection?.Classes.Add("loaded");
        }, DispatcherPriority.Background);

        // Features section: 300ms delay
        Dispatcher.UIThread.Post(async () =>
        {
            await Task.Delay(300);
            featuresSection?.Classes.Add("loaded");
        }, DispatcherPriority.Background);

        // CLI section: 450ms delay
        Dispatcher.UIThread.Post(async () =>
        {
            await Task.Delay(450);
            cliSection?.Classes.Add("loaded");
        }, DispatcherPriority.Background);
    }

    private async void OnCopyCommandClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string command)
        {
            var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
            if (clipboard != null)
            {
                await clipboard.SetTextAsync(command);
            }
        }
    }
}
