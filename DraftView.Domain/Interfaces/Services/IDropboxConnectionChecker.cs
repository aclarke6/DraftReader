using DraftView.Domain.Enumerations;

namespace DraftView.Domain.Interfaces.Services;

public interface IDropboxConnectionChecker
{
    /// <summary>Sets the user whose connection should be checked.</summary>
    void SetUserId(Guid userId);

    Task<DropboxConnectionStatus> GetStatusAsync(CancellationToken ct = default);
    Task<bool> IsConnectedAsync(CancellationToken ct = default);
}
