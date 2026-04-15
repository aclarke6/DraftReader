using DraftView.Domain.Entities;

namespace DraftView.Domain.Tests.Entities;

public class PasswordResetTokenTests
{
    [Fact]
    public void Create_WithUserId_CreatesTokenBoundToUser()
    {
        var userId = Guid.NewGuid();

        var token = PasswordResetToken.Create(userId);

        Assert.NotEqual(Guid.Empty, token.Id);
        Assert.Equal(userId, token.UserId);
        Assert.False(string.IsNullOrWhiteSpace(token.Token));
        Assert.False(token.IsUsed);
        Assert.True(token.ExpiresAt > token.CreatedAt);
    }
}
