namespace DraftView.Application.Contracts;

public sealed record UserEmailAccessResult(
    bool IsAllowed,
    string? Reason = null);
