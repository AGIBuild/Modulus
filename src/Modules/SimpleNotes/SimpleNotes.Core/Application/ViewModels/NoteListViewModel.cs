using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Modulus.UI.Abstractions;
using SimpleNotes.Core.Application;
using SimpleNotes.Core.Domain;

namespace SimpleNotes.Core.Application.ViewModels;

public partial class NoteListViewModel : ViewModelBase
{
    private readonly INoteService _noteService;
    private readonly IUIFactory _uiFactory;
    private readonly IViewHost _viewHost;

    public ObservableCollection<Note> Notes { get; } = new();

    [ObservableProperty]
    private Note? _selectedNote;

    public NoteListViewModel(INoteService noteService, IUIFactory uiFactory, IViewHost viewHost)
    {
        _noteService = noteService;
        _uiFactory = uiFactory;
        _viewHost = viewHost;
        Title = "Simple Notes";
        
        // Load data (fire and forget handled safely?)
        // Better to use RelayCommand for loading or init.
        // For MVP constructor init is acceptable but usually bad practice for async.
        _ = LoadNotesAsync();
    }

    [RelayCommand]
    private async Task LoadNotesAsync()
    {
        var notes = await _noteService.GetNotesAsync();
        Notes.Clear();
        foreach (var note in notes)
        {
            Notes.Add(note);
        }
    }

    [RelayCommand]
    private void AddNote()
    {
         // Implementation for adding a new note
         // e.g. _viewHost.ShowDialogAsync(new NoteEditViewModel(...));
         // For now, just a stub
    }
}
