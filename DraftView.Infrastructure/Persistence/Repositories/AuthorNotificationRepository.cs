using Microsoft.EntityFrameworkCore;
using DraftView.Domain.Entities;
using DraftView.Domain.Interfaces.Repositories;

namespace DraftView.Infrastructure.Persistence.Repositories;

public class AuthorNotificationRepository(DraftViewDbContext db) : IAuthorNotificationRepository
{
    public async Task AddAsync(AuthorNotification notification, CancellationToken ct = default) =>
        await db.AuthorNotifications.AddAsync(notification, ct);

    public async Task<IReadOnlyList<AuthorNotification>> GetByAuthorIdAsync(
        Guid authorId, CancellationToken ct = default) =>
        await db.AuthorNotifications
            .Where(n => n.AuthorId == authorId)
            .OrderByDescending(n => n.OccurredAt)
            .ToListAsync(ct);

    public async Task DeleteAsync(Guid notificationId, CancellationToken ct = default)
    {
        var n = await db.AuthorNotifications.FindAsync([notificationId], ct);
        if (n is not null)
            db.AuthorNotifications.Remove(n);
    }

    public async Task DeleteAllByAuthorIdAsync(Guid authorId, CancellationToken ct = default)
    {
        var all = await db.AuthorNotifications
            .Where(n => n.AuthorId == authorId)
            .ToListAsync(ct);
        db.AuthorNotifications.RemoveRange(all);
    }

    public async Task PruneOlderThanAsync(Guid authorId, DateTime cutoff, CancellationToken ct = default)
    {
        var old = await db.AuthorNotifications
            .Where(n => n.AuthorId == authorId && n.OccurredAt < cutoff)
            .ToListAsync(ct);
        db.AuthorNotifications.RemoveRange(old);
    }
}
