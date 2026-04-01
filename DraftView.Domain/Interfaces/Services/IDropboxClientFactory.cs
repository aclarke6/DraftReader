namespace DraftView.Domain.Interfaces.Services;

public interface IDropboxClientFactory
{
    /// <summary>
    /// Returns an IDropboxClient configured with the access token for the given author.
    /// Refreshes the token automatically if it is near expiry.
    /// Throws InvalidOperationException if no connected DropboxConnection exists for the user.
    /// </summary>
    Task<IDropboxClient> CreateForUserAsync(Guid userId, CancellationToken ct = default);
}
