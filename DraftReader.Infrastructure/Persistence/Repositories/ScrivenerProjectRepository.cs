using Microsoft.EntityFrameworkCore;
using DraftReader.Domain.Entities;
using DraftReader.Domain.Exceptions;
using DraftReader.Domain.Interfaces.Repositories;
using DraftReader.Infrastructure.Persistence;

namespace DraftReader.Infrastructure.Persistence.Repositories;

public class ScrivenerProjectRepository(DraftReaderDbContext db) : IScrivenerProjectRepository
{
    public async Task<ScrivenerProject?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await db.Projects.FindAsync([id], ct);

    public async Task<ScrivenerProject?> GetReaderActiveProjectAsync(CancellationToken ct = default) =>
        await db.Projects.FirstOrDefaultAsync(p => p.IsReaderActive && !p.IsSoftDeleted, ct);

    public async Task<IReadOnlyList<ScrivenerProject>> GetAllAsync(CancellationToken ct = default) =>
        await db.Projects.Where(p => !p.IsSoftDeleted).ToListAsync(ct);

    public async Task<ScrivenerProject?> GetByScrivenerRootUuidAsync(
        string uuid, CancellationToken ct = default) =>
        await db.Projects.FirstOrDefaultAsync(
            p => p.ScrivenerRootUuid == uuid && !p.IsSoftDeleted, ct);

    public async Task<ScrivenerProject?> GetSoftDeletedByScrivenerRootUuidAsync(
        string uuid, CancellationToken ct = default) =>
        await db.Projects.FirstOrDefaultAsync(
            p => p.ScrivenerRootUuid == uuid && p.IsSoftDeleted, ct);

    public async Task AddAsync(ScrivenerProject project, CancellationToken ct = default)
    {
        if (project.ScrivenerRootUuid is not null)
        {
            var existing = await GetByScrivenerRootUuidAsync(project.ScrivenerRootUuid, ct);
            if (existing is not null)
                throw new DuplicateProjectException(project.ScrivenerRootUuid);
        }

        await db.Projects.AddAsync(project, ct);
    }
}

