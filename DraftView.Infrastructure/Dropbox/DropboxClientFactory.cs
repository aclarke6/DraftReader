using System.Text.Json;
using DraftView.Domain.Enumerations;
using DraftView.Domain.Interfaces.Repositories;
using DraftView.Domain.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace DraftView.Infrastructure.Dropbox;

public class DropboxClientFactory(
    IDropboxConnectionRepository connectionRepo,
    IUnitOfWork unitOfWork,
    DropboxClientSettings settings,
    ILogger<DropboxClientFactory> logger) : IDropboxClientFactory
{
    private static readonly HttpClient Http = new();

    public async Task<IDropboxClient> CreateForUserAsync(Guid userId, CancellationToken ct = default)
    {
        var connection = await connectionRepo.GetByUserIdAsync(userId, ct)
            ?? throw new InvalidOperationException(
                $"No Dropbox connection found for user {userId}. " +
                "The author must connect their Dropbox account first.");

        if (connection.Status == DropboxConnectionStatus.Revoked)
            throw new InvalidOperationException(
                "Dropbox access has been revoked. The author must reconnect their Dropbox account.");

        // Refresh token if needed
        if (!connection.IsTokenValid(DateTime.UtcNow))
        {
            if (string.IsNullOrWhiteSpace(connection.RefreshToken))
                throw new InvalidOperationException(
                    "Dropbox access token has expired and no refresh token is available. " +
                    "The author must reconnect their Dropbox account.");

            logger.LogInformation("Refreshing Dropbox token for user {UserId}", userId);
            await RefreshTokenAsync(connection, ct);
            await unitOfWork.SaveChangesAsync(ct);
        }

        return new DropboxClient(new DropboxClientSettings
        {
            AppKey               = settings.AppKey,
            AppSecret            = settings.AppSecret,
            AccessToken          = connection.AccessToken!,
            DropboxScrivenerPath = settings.DropboxScrivenerPath,
            LocalCachePath       = settings.LocalCachePath
        });
    }

    private async Task RefreshTokenAsync(
        Domain.Entities.DropboxConnection connection, CancellationToken ct)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Post,
                "https://api.dropboxapi.com/oauth2/token");

            request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"]    = "refresh_token",
                ["refresh_token"] = connection.RefreshToken!,
                ["client_id"]     = settings.AppKey,
                ["client_secret"] = settings.AppSecret
            });

            var response = await Http.SendAsync(request, ct);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);

            var accessToken = doc.RootElement.GetProperty("access_token").GetString()!;
            var expiresIn   = doc.RootElement.GetProperty("expires_in").GetInt32();
            var expiresAt   = DateTime.UtcNow.AddSeconds(expiresIn);

            connection.UpdateAccessToken(accessToken, expiresAt);
            logger.LogInformation("Dropbox token refreshed successfully for user {UserId}",
                connection.UserId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to refresh Dropbox token for user {UserId}",
                connection.UserId);
            connection.MarkTokenExpired();
            throw new InvalidOperationException(
                "Failed to refresh Dropbox access token. The author may need to reconnect.", ex);
        }
    }
}
