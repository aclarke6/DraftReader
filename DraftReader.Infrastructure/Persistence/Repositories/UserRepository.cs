using Microsoft.EntityFrameworkCore;
using DraftReader.Domain.Entities;
using DraftReader.Domain.Enumerations;
using DraftReader.Domain.Interfaces.Repositories;
using DraftReader.Infrastructure.Persistence;

namespace DraftReader.Infrastructure.Persistence.Repositories;

public class UserRepository(DraftReaderDbContext db) : IUserRepository
{
    public async Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await db.AppUsers.FindAsync([id], ct);

    public async Task<User?> GetByEmailAsync(string email, CancellationToken ct = default) =>
        await db.AppUsers.FirstOrDefaultAsync(u => u.Email == email, ct);

    public async Task<User?> GetAuthorAsync(CancellationToken ct = default) =>
        await db.AppUsers.FirstOrDefaultAsync(u => u.Role == Role.Author, ct);

    public async Task<IReadOnlyList<User>> GetAllBetaReadersAsync(CancellationToken ct = default) =>
        await db.AppUsers.Where(u => u.Role == Role.BetaReader).ToListAsync(ct);

    public async Task<int> CountActiveBetaReadersAsync(CancellationToken ct = default) =>
        await db.AppUsers.CountAsync(u =>
            u.Role == Role.BetaReader &&
            u.IsActive &&
            !u.IsSoftDeleted, ct);

    public async Task AddAsync(User user, CancellationToken ct = default) =>
        await db.AppUsers.AddAsync(user, ct);

    public async Task<bool> EmailExistsAsync(string email, CancellationToken ct = default) =>
        await db.AppUsers.AnyAsync(u => u.Email == email, ct);
}
