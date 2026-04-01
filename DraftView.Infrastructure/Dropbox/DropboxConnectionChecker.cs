using DraftView.Domain.Enumerations;
using DraftView.Domain.Interfaces.Repositories;
using DraftView.Domain.Interfaces.Services;

namespace DraftView.Infrastructure.Dropbox;

/// <summary>
/// Checks the Dropbox connection status for a specific user from the database.
/// In single-author mode, pass the author's UserId.
/// </summary>
public class DropboxConnectionChecker(IDropboxConnectionRepository connectionRepo)
    : IDropboxConnectionChecker
{
    private Guid _userId;

    public void SetUserId(Guid userId) => _userId = userId;

    public async Task<DropboxConnectionStatus> GetStatusAsync(CancellationToken ct = default)
    {
        if (_userId == Guid.Empty)
            return DropboxConnectionStatus.NotConnected;

        var connection = await connectionRepo.GetByUserIdAsync(_userId, ct);

        if (connection is null)
            return DropboxConnectionStatus.NotConnected;

        return connection.Status;
    }

    public async Task<bool> IsConnectedAsync(CancellationToken ct = default)
    {
        var status = await GetStatusAsync(ct);
        return status == DropboxConnectionStatus.Connected;
    }
}
