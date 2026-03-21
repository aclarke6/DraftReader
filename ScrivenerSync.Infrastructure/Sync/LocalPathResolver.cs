using ScrivenerSync.Domain.Entities;
using ScrivenerSync.Domain.Interfaces.Services;

namespace ScrivenerSync.Infrastructure.Sync;

/// <summary>
/// Resolves the local .scriv path for a project.
/// When a Dropbox client is available, downloads the project to the local cache first.
/// When no Dropbox client is available, uses DropboxPath directly as a local path.
/// </summary>
public class LocalPathResolver(
    IDropboxClient? dropboxClient,
    DropboxClientSettingsAccessor settings) : ILocalPathResolver
{
    public async Task<string> ResolveAsync(
        ScrivenerProject project, CancellationToken ct = default)
    {
        if (dropboxClient is null)
            return project.DropboxPath;

        var cachePath = Path.Combine(
            settings.LocalCachePath,
            Path.GetFileName(project.DropboxPath));

        await dropboxClient.DownloadFolderAsync(project.DropboxPath, cachePath, ct);
        return cachePath;
    }

    public async Task<string> ResolveScrivxAsync(
        ScrivenerProject project, CancellationToken ct = default)
    {
        var localPath   = await ResolveAsync(project, ct);
        var scrivxFiles = Directory.GetFiles(localPath, "*.scrivx");

        if (scrivxFiles.Length == 0)
            throw new InvalidOperationException(
                $"No .scrivx file found in {localPath}");

        return scrivxFiles[0];
    }
}

/// <summary>Simple settings accessor to avoid circular DI dependencies.</summary>
public class DropboxClientSettingsAccessor
{
    public string LocalCachePath { get; init; } = string.Empty;
}
