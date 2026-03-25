using Microsoft.EntityFrameworkCore;
using DraftReader.Domain.Entities;
using DraftReader.Domain.Interfaces.Repositories;
using DraftReader.Infrastructure.Persistence;

namespace DraftReader.Infrastructure.Persistence.Repositories;

public class InvitationRepository(DraftReaderDbContext db) : IInvitationRepository
{
    public async Task<Invitation?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await db.Invitations.FindAsync([id], ct);

    public async Task<Invitation?> GetByTokenAsync(string token, CancellationToken ct = default) =>
        await db.Invitations.FirstOrDefaultAsync(i => i.Token == token, ct);

    public async Task<Invitation?> GetByUserIdAsync(Guid userId, CancellationToken ct = default) =>
        await db.Invitations.FirstOrDefaultAsync(i => i.UserId == userId, ct);

    public async Task AddAsync(Invitation invitation, CancellationToken ct = default) =>
        await db.Invitations.AddAsync(invitation, ct);
}
