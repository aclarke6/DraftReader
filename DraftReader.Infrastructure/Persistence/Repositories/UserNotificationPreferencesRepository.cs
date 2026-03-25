using Microsoft.EntityFrameworkCore;
using DraftReader.Domain.Entities;
using DraftReader.Domain.Interfaces.Repositories;
using DraftReader.Infrastructure.Persistence;

namespace DraftReader.Infrastructure.Persistence.Repositories;

public class UserNotificationPreferencesRepository(DraftReaderDbContext db) : IUserNotificationPreferencesRepository
{
    public async Task<UserNotificationPreferences?> GetByUserIdAsync(Guid userId, CancellationToken ct = default) =>
        await db.NotificationPreferences.FirstOrDefaultAsync(p => p.UserId == userId, ct);

    public async Task AddAsync(UserNotificationPreferences preferences, CancellationToken ct = default) =>
        await db.NotificationPreferences.AddAsync(preferences, ct);
}
