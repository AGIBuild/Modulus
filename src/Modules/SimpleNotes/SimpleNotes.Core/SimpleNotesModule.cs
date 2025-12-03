using Microsoft.Extensions.DependencyInjection;
using Modulus.Sdk;
using SimpleNotes.Core.Application;
using SimpleNotes.Core.Application.ViewModels;

namespace SimpleNotes.Core;

/// <summary>
/// Simple Notes Core - business logic only.
/// UI-specific menu declarations are in UI.Avalonia and UI.Blazor modules.
/// </summary>
[Module("SimpleNotes", "Notes",
    Description = "A simple note taking module to demonstrate Modulus vertical slice architecture.")]
public class SimpleNotesModule : ModuleBase
{
    public override void ConfigureServices(IModuleLifecycleContext context)
    {
        context.Services.AddSingleton<INoteService, NoteService>();
        context.Services.AddTransient<NoteListViewModel>();
    }
}
