using System;
using System.IO;
using Xunit;

namespace Modulus.Cli.IntegrationTests;

public class TemplateConventionsTests
{
    [Fact]
    public void Templates_Match_ViewModelBase_And_ViewMenu_Conventions()
    {
        var repoRoot = FindRepoRoot();

        // CLI templates (used by `modulus new`)
        AssertContains(
            Read(repoRoot, "src", "Modulus.Cli", "Templates", "Core", "ViewModels", "MainViewModel.cs.template"),
            ": ViewModelBase");
        AssertContains(
            Read(repoRoot, "src", "Modulus.Cli", "Templates", "Core", "Module.cs.template"),
            "AddTransient<ViewModels.MainViewModel>()");
        AssertContains(
            Read(repoRoot, "src", "Modulus.Cli", "Templates", "Avalonia", "MainView.axaml.cs.template"),
            "public MainView(MainViewModel vm)");
        AssertContains(
            Read(repoRoot, "src", "Modulus.Cli", "Templates", "Avalonia", "MainView.axaml.cs.template"),
            "Design.IsDesignMode");
        AssertContains(
            Read(repoRoot, "src", "Modulus.Cli", "Templates", "Blazor", "MainView.razor.template"),
            "[BlazorViewMenu(");
        AssertContains(
            Read(repoRoot, "src", "Modulus.Cli", "Templates", "Blazor", "MainView.razor.template"),
            "@page");

        // DotnetNew templates
        AssertContains(
            Read(repoRoot, "templates", "DotnetNew", "ModulusModule.Avalonia", "ModulusModule.Core", "ViewModels", "MainViewModel.cs"),
            ": ViewModelBase");
        AssertContains(
            Read(repoRoot, "templates", "DotnetNew", "ModulusModule.Blazor", "ModulusModule.Core", "ViewModels", "MainViewModel.cs"),
            ": ViewModelBase");
        AssertContains(
            Read(repoRoot, "templates", "DotnetNew", "ModulusModule.Blazor", "ModulusModule.UI.Blazor", "MainView.razor"),
            "[BlazorViewMenu(");

        // VisualStudio templates
        AssertContains(
            Read(repoRoot, "templates", "VisualStudio", "ModulusModule.Avalonia", "Core", "ViewModels", "MainViewModel.cs"),
            ": ViewModelBase");
        AssertContains(
            Read(repoRoot, "templates", "VisualStudio", "ModulusModule.Blazor", "Core", "ViewModels", "MainViewModel.cs"),
            ": ViewModelBase");
        AssertContains(
            Read(repoRoot, "templates", "VisualStudio", "ModulusModule.Blazor", "UI.Blazor", "MainView.razor"),
            "[BlazorViewMenu(");

        // Must not regress to the removed base class
        AssertDoesNotContain(
            Read(repoRoot, "src", "Modulus.Cli", "Templates", "Core", "ViewModels", "MainViewModel.cs.template"),
            "NavigationViewModelBase");
    }

    private static string Read(params string[] parts)
    {
        var path = Path.Combine(parts);
        Assert.True(File.Exists(path), $"Expected file to exist: {path}");
        return File.ReadAllText(path);
    }

    private static void AssertContains(string text, string needle)
        => Assert.Contains(needle, text, StringComparison.Ordinal);

    private static void AssertDoesNotContain(string text, string needle)
        => Assert.DoesNotContain(needle, text, StringComparison.Ordinal);

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            var sln = Path.Combine(dir.FullName, "Modulus.sln");
            if (File.Exists(sln))
                return dir.FullName;
            dir = dir.Parent;
        }

        throw new InvalidOperationException("Could not locate repo root (Modulus.sln not found).");
    }
}


