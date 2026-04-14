using DraftView.Domain.Enumerations;

namespace DraftView.Application.Contracts;

public sealed record UserEmailAccessRequest(
    Guid RequestingUserId,
    Role RequestingUserRole,
    Guid TargetUserId,
    UserEmailAccessPurpose Purpose);
