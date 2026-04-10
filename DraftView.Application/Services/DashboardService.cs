using DraftView.Domain.Entities;
using DraftView.Domain.Interfaces.Repositories;
using DraftView.Domain.Interfaces.Services;

namespace DraftView.Application.Services;

public class DashboardService(
    ISectionRepository sectionRepo,
    IUserRepository userRepo,
    IEmailDeliveryLogRepository logRepo,
    IAuthorNotificationRepository notificationRepo,
    IUnitOfWork unitOfWork) : IDashboardService
{
    public async Task<IReadOnlyList<Section>> GetProjectOverviewAsync(
        Guid projectId, CancellationToken ct = default) =>
        await sectionRepo.GetPublishedByProjectIdAsync(projectId, ct);

    public async Task<IReadOnlyList<User>> GetReaderSummaryAsync(
        CancellationToken ct = default) =>
        await userRepo.GetAllBetaReadersAsync(ct);

    public async Task<IReadOnlyList<EmailDeliveryLog>> GetEmailHealthSummaryAsync(
        CancellationToken ct = default) =>
        await logRepo.GetFailedAsync(ct);

    public async Task<IReadOnlyList<AuthorNotification>> GetNotificationsAsync(
        Guid authorId, CancellationToken ct = default)
    {
        await notificationRepo.PruneOlderThanAsync(authorId, DateTime.UtcNow.AddDays(-90), ct);
        return await notificationRepo.GetByAuthorIdAsync(authorId, ct);
    }

    public async Task DismissNotificationAsync(
        Guid notificationId, CancellationToken ct = default)
    {
        await notificationRepo.DeleteAsync(notificationId, ct);
        await unitOfWork.SaveChangesAsync(ct);
    }

    public async Task DismissAllNotificationsAsync(
        Guid authorId, CancellationToken ct = default)
    {
        await notificationRepo.DeleteAllByAuthorIdAsync(authorId, ct);
        await unitOfWork.SaveChangesAsync(ct);
    }
}
