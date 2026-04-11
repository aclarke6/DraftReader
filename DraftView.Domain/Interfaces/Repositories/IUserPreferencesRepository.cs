using DraftView.Domain.Entities;

namespace DraftView.Domain.Interfaces.Repositories;

public interface IUserPreferencesRepository
{
    Task<UserPreferences?> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task AddAsync(UserPreferences preferences, CancellationToken ct = default);
}
