using DraftReader.Domain.Entities;

namespace DraftReader.Domain.Interfaces.Repositories;

public interface ICommentRepository
{
    Task<Comment?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Comment>> GetRootsBySectionIdAsync(Guid sectionId, CancellationToken ct = default);
    Task<IReadOnlyList<Comment>> GetRepliesByParentIdAsync(Guid parentCommentId, CancellationToken ct = default);
    Task<IReadOnlyList<Comment>> GetByAuthorIdAsync(Guid authorId, CancellationToken ct = default);
    Task<int> CountBySectionIdAsync(Guid sectionId, CancellationToken ct = default);
    Task AddAsync(Comment comment, CancellationToken ct = default);
}
