using Microsoft.Extensions.DependencyInjection;
using Modulus.Sdk;
using Modulus.UI.Abstractions;
using SimpleNotes.Core;
using SimpleNotes.Core.Application.ViewModels;

namespace SimpleNotes.UI.Blazor;

/// <summary>
/// Simple Notes Blazor UI - declares Blazor-specific navigation.
/// </summary>
[DependsOn(typeof(SimpleNotesModule))]
[BlazorMenu("Notes", "/notes", Icon = IconKind.Document, Order = 30)]
public class SimpleNotesBlazorModule : ModulusComponent
{
    public override Task OnApplicationInitializationAsync(IModuleInitializationContext context, CancellationToken cancellationToken = default)
    {
        // Register View-ViewModel mapping (if needed for Blazor)
        var viewRegistry = context.ServiceProvider.GetService<IViewRegistry>();
        viewRegistry?.Register<NoteListViewModel, NoteListView>();
        return Task.CompletedTask;
    }
}
