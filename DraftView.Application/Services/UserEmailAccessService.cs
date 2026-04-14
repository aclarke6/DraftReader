using DraftView.Application.Contracts;
using DraftView.Application.Interfaces;

namespace DraftView.Application.Services;

public sealed class UserEmailAccessService : IUserEmailAccessService
{
    public Task<UserEmailAccessResult> EvaluateAccessAsync(
        UserEmailAccessRequest request,
        CancellationToken ct = default) =>
        throw new NotImplementedException("Stage 2 tests should drive IUserEmailAccessService behaviour.");
}
