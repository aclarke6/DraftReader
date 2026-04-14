using DraftView.Application.Contracts;

namespace DraftView.Application.Interfaces;

public interface IUserEmailAccessService
{
    Task<UserEmailAccessResult> EvaluateAccessAsync(
        UserEmailAccessRequest request,
        CancellationToken ct = default);
}
