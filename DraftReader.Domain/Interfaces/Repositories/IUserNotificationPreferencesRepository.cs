using DraftReader.Domain.Entities;

namespace DraftReader.Domain.Interfaces.Repositories;

public interface IUserNotificationPreferencesRepository
{
    Task<UserNotificationPreferences?> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task AddAsync(UserNotificationPreferences preferences, CancellationToken ct = default);
}
