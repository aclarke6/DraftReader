using DraftView.Domain.Entities;

namespace DraftView.Domain.Interfaces.Services;

public interface IDashboardService
{
    Task<IReadOnlyList<Section>> GetProjectOverviewAsync(Guid projectId, CancellationToken ct = default);
    Task<IReadOnlyList<User>> GetReaderSummaryAsync(CancellationToken ct = default);
    Task<IReadOnlyList<EmailDeliveryLog>> GetEmailHealthSummaryAsync(CancellationToken ct = default);

    Task<IReadOnlyList<AuthorNotification>> GetNotificationsAsync(
        Guid authorId, CancellationToken ct = default);

    Task DismissNotificationAsync(
        Guid notificationId, CancellationToken ct = default);

    Task DismissAllNotificationsAsync(
        Guid authorId, CancellationToken ct = default);
}
