using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SimpleNotes.Core.Domain;

namespace SimpleNotes.Core.Application;

public class NoteService : INoteService
{
    private readonly ConcurrentDictionary<Guid, Note> _notes = new();

    public NoteService()
    {
        // Seed some data
        var note1 = new Note { Title = "Welcome", Content = "Welcome to Modulus SimpleNotes!" };
        _notes.TryAdd(note1.Id, note1);
        
        var note2 = new Note { Title = "Architecture", Content = "Remember the Pyramid Layering." };
        _notes.TryAdd(note2.Id, note2);
    }

    public Task<List<Note>> GetNotesAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_notes.Values.OrderByDescending(n => n.ModifiedAt).ToList());
    }

    public Task<Note> CreateNoteAsync(string title, string content, CancellationToken cancellationToken = default)
    {
        var note = new Note
        {
            Title = title,
            Content = content
        };
        _notes.TryAdd(note.Id, note);
        return Task.FromResult(note);
    }

    public Task UpdateNoteAsync(Note note, CancellationToken cancellationToken = default)
    {
        if (_notes.TryGetValue(note.Id, out var existing))
        {
            existing.Title = note.Title;
            existing.Content = note.Content;
            existing.ModifiedAt = DateTime.UtcNow;
        }
        return Task.CompletedTask;
    }

    public Task DeleteNoteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _notes.TryRemove(id, out _);
        return Task.CompletedTask;
    }
}

