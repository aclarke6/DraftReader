using DraftView.Domain.Entities;
using DraftView.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DraftView.Infrastructure.Persistence.Repositories;

public class DropboxConnectionRepository(DraftViewDbContext db) : IDropboxConnectionRepository
{
    public Task<DropboxConnection?> GetByUserIdAsync(Guid userId, CancellationToken ct = default) =>
        db.DropboxConnections
            .FirstOrDefaultAsync(d => d.UserId == userId, ct);

    public async Task AddAsync(DropboxConnection connection, CancellationToken ct = default) =>
        await db.DropboxConnections.AddAsync(connection, ct);
}
