namespace DraftView.Domain.Entities;

public class PasswordResetToken
{
    public Guid   Id        { get; private set; }
    public Guid   UserId    { get; private set; }
    public string Token     { get; private set; } = string.Empty;
    public DateTime ExpiresAt { get; private set; }
    public bool   IsUsed    { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private PasswordResetToken() { }

    public static PasswordResetToken Create(Guid userId)
    {
        return new PasswordResetToken
        {
            Id        = Guid.NewGuid(),
            UserId    = userId,
            Token     = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32))
                            .Replace("+", "-").Replace("/", "_").Replace("=", ""),
            ExpiresAt = DateTime.UtcNow.AddHours(24),
            IsUsed    = false,
            CreatedAt = DateTime.UtcNow
        };
    }

    public bool IsValid() => !IsUsed && DateTime.UtcNow < ExpiresAt;

    public void MarkUsed() => IsUsed = true;
}
