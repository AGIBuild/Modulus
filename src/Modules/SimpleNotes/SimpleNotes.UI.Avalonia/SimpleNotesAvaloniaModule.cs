using Microsoft.Extensions.DependencyInjection;
using Modulus.Sdk;
using Modulus.UI.Abstractions;
using SimpleNotes.Core;
using SimpleNotes.Core.Application.ViewModels;

namespace SimpleNotes.UI.Avalonia;

/// <summary>
/// Simple Notes Avalonia UI - declares Avalonia-specific navigation.
/// </summary>
[DependsOn(typeof(SimpleNotesModule))]
[AvaloniaMenu("Notes", typeof(NoteListViewModel), Icon = "📝", Order = 30)]
public class SimpleNotesAvaloniaModule : ModuleBase
{
    public override Task OnApplicationInitializationAsync(IModuleInitializationContext context, CancellationToken cancellationToken = default)
    {
        // Register View-ViewModel mapping
        var viewRegistry = context.ServiceProvider.GetRequiredService<IViewRegistry>();
        viewRegistry.Register<NoteListViewModel, NoteListView>();
        return Task.CompletedTask;
    }
}
