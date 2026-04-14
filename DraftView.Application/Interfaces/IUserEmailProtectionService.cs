namespace DraftView.Application.Interfaces;

public interface IUserEmailProtectionService
{
    Task<string> GetEmailAsync(Guid targetUserId, CancellationToken ct = default);
}
