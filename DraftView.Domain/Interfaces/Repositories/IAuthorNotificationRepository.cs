using DraftView.Domain.Entities;

namespace DraftView.Domain.Interfaces.Repositories;

public interface IAuthorNotificationRepository
{
    Task AddAsync(AuthorNotification notification, CancellationToken ct = default);
    Task<IReadOnlyList<AuthorNotification>> GetByAuthorIdAsync(Guid authorId, CancellationToken ct = default);
    Task DeleteAsync(Guid notificationId, CancellationToken ct = default);
    Task DeleteAllByAuthorIdAsync(Guid authorId, CancellationToken ct = default);
    Task PruneOlderThanAsync(Guid authorId, DateTime cutoff, CancellationToken ct = default);
}
