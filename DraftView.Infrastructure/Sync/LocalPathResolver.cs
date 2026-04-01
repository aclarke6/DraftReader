using DraftView.Domain.Entities;
using DraftView.Domain.Interfaces.Services;

namespace DraftView.Infrastructure.Sync;

public class LocalPathResolver : ILocalPathResolver
{
    private readonly string _localCachePath;
    private Guid _userId;

    public LocalPathResolver(string localCachePath)
    {
        _localCachePath = localCachePath;
    }

    public void SetUserId(Guid userId) => _userId = userId;

    public Task<string> ResolveAsync(ScrivenerProject project, CancellationToken ct = default)
    {
        string basePath;

        if (string.IsNullOrWhiteSpace(_localCachePath))
        {
            basePath = project.DropboxPath;
        }
        else
        {
            // Scope cache path per user: {cachePath}/{userId}/{projectFolder}
            var userCachePath = _userId != Guid.Empty
                ? Path.Combine(_localCachePath, _userId.ToString())
                : _localCachePath;

            basePath = Path.Combine(
                userCachePath,
                Path.GetFileName(project.DropboxPath.TrimEnd('/').TrimEnd('\\')));
        }

        return Task.FromResult(basePath);
    }

    public async Task<string> ResolveScrivxAsync(ScrivenerProject project, CancellationToken ct = default)
    {
        var vaultPath = await ResolveAsync(project, ct);
        var dirName   = Path.GetFileNameWithoutExtension(vaultPath);
        return Path.Combine(vaultPath, dirName + ".scrivx");
    }
}
