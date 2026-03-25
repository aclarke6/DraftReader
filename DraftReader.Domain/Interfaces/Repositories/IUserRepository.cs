using DraftReader.Domain.Entities;

namespace DraftReader.Domain.Interfaces.Repositories;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<User?> GetAuthorAsync(CancellationToken ct = default);
    Task<IReadOnlyList<User>> GetAllBetaReadersAsync(CancellationToken ct = default);
    Task<int> CountActiveBetaReadersAsync(CancellationToken ct = default);
    Task AddAsync(User user, CancellationToken ct = default);
    Task<bool> EmailExistsAsync(string email, CancellationToken ct = default);
}
