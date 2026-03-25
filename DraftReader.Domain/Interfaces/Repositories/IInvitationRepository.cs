using DraftReader.Domain.Entities;

namespace DraftReader.Domain.Interfaces.Repositories;

public interface IInvitationRepository
{
    Task<Invitation?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Invitation?> GetByTokenAsync(string token, CancellationToken ct = default);
    Task<Invitation?> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task AddAsync(Invitation invitation, CancellationToken ct = default);
}
