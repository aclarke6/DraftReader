using DraftView.Domain.Entities;

namespace DraftView.Domain.Interfaces.Repositories;

public interface IDropboxConnectionRepository
{
    /// <summary>Gets the DropboxConnection for a user, or null if none exists.</summary>
    Task<DropboxConnection?> GetByUserIdAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Adds a new DropboxConnection.</summary>
    Task AddAsync(DropboxConnection connection, CancellationToken ct = default);
}
