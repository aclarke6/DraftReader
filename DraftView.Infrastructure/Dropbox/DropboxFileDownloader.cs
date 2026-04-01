using DraftView.Domain.Entities;
using DraftView.Domain.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace DraftView.Infrastructure.Dropbox;

public class DropboxFileDownloader(
    IDropboxClientFactory clientFactory,
    ILocalPathResolver pathResolver,
    ILogger<DropboxFileDownloader> logger) : IDropboxFileDownloader
{
    public async Task<string> DownloadProjectAsync(
        ScrivenerProject project,
        Guid userId,
        CancellationToken ct = default)
    {
        pathResolver.SetUserId(userId);
        var localPath = await pathResolver.ResolveAsync(project, ct);

        logger.LogInformation(
            "Downloading project {Name} from {DropboxPath} to {LocalPath}",
            project.Name, project.DropboxPath, localPath);

        var client = await clientFactory.CreateForUserAsync(userId, ct);
        await client.DownloadFolderAsync(project.DropboxPath, localPath, ct);

        logger.LogInformation(
            "Downloaded project {Name} successfully", project.Name);

        return localPath;
    }
}
