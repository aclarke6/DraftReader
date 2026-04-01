using DraftView.Domain.Entities;

namespace DraftView.Domain.Interfaces.Services;

public interface ILocalPathResolver
{
    /// <summary>Sets the user whose cache path should be used.</summary>
    void SetUserId(Guid userId);

    Task<string> ResolveAsync(ScrivenerProject project, CancellationToken ct = default);
    Task<string> ResolveScrivxAsync(ScrivenerProject project, CancellationToken ct = default);
}
