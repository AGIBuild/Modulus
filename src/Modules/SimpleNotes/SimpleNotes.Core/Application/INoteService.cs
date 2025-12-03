using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SimpleNotes.Core.Domain;

namespace SimpleNotes.Core.Application;

public interface INoteService
{
    Task<List<Note>> GetNotesAsync(CancellationToken cancellationToken = default);
    Task<Note> CreateNoteAsync(string title, string content, CancellationToken cancellationToken = default);
    Task UpdateNoteAsync(Note note, CancellationToken cancellationToken = default);
    Task DeleteNoteAsync(Guid id, CancellationToken cancellationToken = default);
}

