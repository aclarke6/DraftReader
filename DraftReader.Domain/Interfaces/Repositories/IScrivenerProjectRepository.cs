using DraftReader.Domain.Entities;

namespace DraftReader.Domain.Interfaces.Repositories;

public interface IScrivenerProjectRepository
{
    Task<ScrivenerProject?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<ScrivenerProject?> GetReaderActiveProjectAsync(CancellationToken ct = default);
    Task<IReadOnlyList<ScrivenerProject>> GetAllAsync(CancellationToken ct = default);
    Task<ScrivenerProject?> GetByScrivenerRootUuidAsync(string uuid, CancellationToken ct = default);
    Task<ScrivenerProject?> GetSoftDeletedByScrivenerRootUuidAsync(string uuid, CancellationToken ct = default);
    Task AddAsync(ScrivenerProject project, CancellationToken ct = default);
}


