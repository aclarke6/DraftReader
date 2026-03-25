using DraftReader.Domain.Enumerations;
using DraftReader.Domain.Interfaces.Services;

namespace DraftReader.Infrastructure.Dropbox;

public class DropboxConnectionChecker(DropboxClientSettings settings) : IDropboxConnectionChecker
{
    public Task<DropboxConnectionStatus> GetStatusAsync(CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(settings.AccessToken))
            return Task.FromResult(DropboxConnectionStatus.NotConnected);

        return Task.FromResult(DropboxConnectionStatus.Connected);
    }

    public async Task<bool> IsConnectedAsync(CancellationToken ct = default)
    {
        var status = await GetStatusAsync(ct);
        return status == DropboxConnectionStatus.Connected;
    }
}
