using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Modulus.Modules.Home.Services;
using Modulus.UI.Abstractions;
using System.Diagnostics;

namespace Modulus.Modules.Home.ViewModels;

public partial class HomeViewModel : ViewModelBase
{
    private readonly IHomeStatisticsService _statisticsService;

    [ObservableProperty]
    private bool _isLoading = true;

    [ObservableProperty]
    private int _installedModuleCount;

    [ObservableProperty]
    private int _runningModuleCount;

    [ObservableProperty]
    private string _hostType = "";

    public HomeViewModel(IHomeStatisticsService statisticsService)
    {
        _statisticsService = statisticsService;
        Title = "Home";
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            var stats = await _statisticsService.GetStatisticsAsync();
            InstalledModuleCount = stats.InstalledModuleCount;
            RunningModuleCount = stats.RunningModuleCount;
            HostType = stats.HostType;
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void OpenGitHub()
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = GitHubUrl,
                UseShellExecute = true
            });
        }
        catch
        {
            // Ignore errors opening browser
        }
    }

    /// <summary>
    /// GitHub repository URL.
    /// </summary>
    public string GitHubUrl { get; } = "https://github.com/agibuild/Modulus";

    /// <summary>
    /// CLI commands to display in quick start section.
    /// </summary>
    public IReadOnlyList<CliCommand> QuickStartCommands { get; } =
    [
        new("Create a new module", "modulus new MyModule -t avalonia"),
        new("Build all modules", "modulus build"),
        new("Run with hot reload", "modulus run --watch"),
        new("Install a module", "modulus install ./MyModule.modpkg")
    ];

    /// <summary>
    /// Framework features to display.
    /// </summary>
    public IReadOnlyList<FeatureItem> Features { get; } =
    [
        new("🎯", "Multi-Host", "Run on Avalonia, Blazor, or custom hosts"),
        new("⚡", "Hot Reload", "Live module updates without restart"),
        new("🔧", "VS Compatible", "Familiar extension manifest format"),
        new("🤖", "AI Ready", "Built-in AI agent integration points")
    ];
}

public record CliCommand(string Description, string Command);

public record FeatureItem(string Icon, string Title, string Description);
