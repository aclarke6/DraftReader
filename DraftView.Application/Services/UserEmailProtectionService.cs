using DraftView.Application.Interfaces;

namespace DraftView.Application.Services;

public sealed class UserEmailProtectionService : IUserEmailProtectionService
{
    public Task<string> GetEmailAsync(Guid targetUserId, CancellationToken ct = default) =>
        throw new NotImplementedException("Stage 2 tests should drive IUserEmailProtectionService behaviour.");
}
