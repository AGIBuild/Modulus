using Microsoft.Extensions.DependencyInjection;
using Modulus.Sdk;
using Modulus.UI.Abstractions;
using Modulus.UI.Avalonia.Infrastructure;
using SimpleNotes.Core;
using SimpleNotes.Core.Application.ViewModels;

namespace SimpleNotes.UI.Avalonia;

/// <summary>
/// Simple Notes Avalonia UI - declares Avalonia-specific navigation.
/// </summary>
[DependsOn(typeof(SimpleNotesModule))]
[AvaloniaMenu("Notes", typeof(NoteListViewModel), Icon = IconKind.Document, Order = 30)]
public class SimpleNotesAvaloniaModule : AvaloniaModuleBase
{
    public override async Task OnApplicationInitializationAsync(IModuleInitializationContext context, CancellationToken cancellationToken = default)
    {
        await base.OnApplicationInitializationAsync(context, cancellationToken);

        // Register View-ViewModel mapping
        var viewRegistry = context.ServiceProvider.GetRequiredService<IViewRegistry>();
        viewRegistry.Register<NoteListViewModel, NoteListView>();
    }
}
