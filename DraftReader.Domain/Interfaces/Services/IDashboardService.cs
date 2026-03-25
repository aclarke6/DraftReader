using DraftReader.Domain.Entities;

namespace DraftReader.Domain.Interfaces.Services;

public interface IDashboardService
{
    Task<IReadOnlyList<Section>> GetProjectOverviewAsync(Guid projectId, CancellationToken ct = default);
    Task<IReadOnlyList<User>> GetReaderSummaryAsync(CancellationToken ct = default);
    Task<IReadOnlyList<EmailDeliveryLog>> GetEmailHealthSummaryAsync(CancellationToken ct = default);
}
