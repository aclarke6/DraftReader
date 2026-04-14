using DraftView.Application.Interfaces;
using DraftView.Domain.Entities;

namespace DraftView.Application.Services;

public sealed class AuthenticationUserLookupService : IAuthenticationUserLookupService
{
    public Task<User?> FindByLoginEmailAsync(string emailInput, CancellationToken ct = default) =>
        throw new NotImplementedException("Stage 2 tests should drive authentication lookup behaviour.");
}
