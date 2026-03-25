using Microsoft.EntityFrameworkCore;
using DraftReader.Domain.Entities;
using DraftReader.Domain.Interfaces.Repositories;
using DraftReader.Infrastructure.Persistence;

namespace DraftReader.Infrastructure.Persistence.Repositories;

public class CommentRepository(DraftReaderDbContext db) : ICommentRepository
{
    public async Task<Comment?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await db.Comments.FindAsync([id], ct);

    public async Task<IReadOnlyList<Comment>> GetRootsBySectionIdAsync(Guid sectionId, CancellationToken ct = default) =>
        await db.Comments
            .Where(c => c.SectionId == sectionId && c.ParentCommentId == null)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Comment>> GetRepliesByParentIdAsync(Guid parentCommentId, CancellationToken ct = default) =>
        await db.Comments
            .Where(c => c.ParentCommentId == parentCommentId)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Comment>> GetByAuthorIdAsync(Guid authorId, CancellationToken ct = default) =>
        await db.Comments
            .Where(c => c.AuthorId == authorId)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync(ct);

    public async Task<int> CountBySectionIdAsync(Guid sectionId, CancellationToken ct = default) =>
        await db.Comments.CountAsync(c => c.SectionId == sectionId && !c.IsSoftDeleted, ct);

    public async Task AddAsync(Comment comment, CancellationToken ct = default) =>
        await db.Comments.AddAsync(comment, ct);
}
