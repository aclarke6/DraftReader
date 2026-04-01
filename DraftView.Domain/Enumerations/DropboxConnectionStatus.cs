namespace DraftView.Domain.Enumerations;

public enum DropboxConnectionStatus
{
    NotConnected,
    Connected,
    TokenExpired,
    Revoked,
    Error
}
