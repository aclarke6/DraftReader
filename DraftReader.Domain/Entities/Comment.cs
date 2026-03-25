using DraftReader.Domain.Enumerations;
using DraftReader.Domain.Exceptions;

namespace DraftReader.Domain.Entities;

public sealed class Comment
{
    // ---------------------------------------------------------------------------
    // Properties
    // ---------------------------------------------------------------------------

    public Guid Id { get; private set; }
    public Guid SectionId { get; private set; }
    public Guid AuthorId { get; private set; }
    public Guid? ParentCommentId { get; private set; }
    public string Body { get; private set; } = default!;
    public Visibility Visibility { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? EditedAt { get; private set; }
    public bool IsSoftDeleted { get; private set; }
    public DateTime? SoftDeletedAt { get; private set; }

    // ---------------------------------------------------------------------------
    // Constructor
    // ---------------------------------------------------------------------------

    private Comment() { }

    // ---------------------------------------------------------------------------
    // Factories
    // ---------------------------------------------------------------------------

    public static Comment CreateRoot(
        Guid sectionId,
        Guid authorId,
        string body,
        Visibility visibility)
    {
        ValidateBody(body);

        return new Comment
        {
            Id              = Guid.NewGuid(),
            SectionId       = sectionId,
            AuthorId        = authorId,
            ParentCommentId = null,
            Body            = body.Trim(),
            Visibility      = visibility,
            CreatedAt       = DateTime.UtcNow,
            IsSoftDeleted   = false
        };
    }

    public static Comment CreateReply(
        Guid sectionId,
        Guid authorId,
        Guid parentCommentId,
        Visibility parentVisibility,
        string body,
        Visibility requestedVisibility)
    {
        ValidateBody(body);

        // I-03: private parent forces private visibility on all replies
        var effectiveVisibility = parentVisibility == Visibility.Private
            ? Visibility.Private
            : requestedVisibility;

        return new Comment
        {
            Id              = Guid.NewGuid(),
            SectionId       = sectionId,
            AuthorId        = authorId,
            ParentCommentId = parentCommentId,
            Body            = body.Trim(),
            Visibility      = effectiveVisibility,
            CreatedAt       = DateTime.UtcNow,
            IsSoftDeleted   = false
        };
    }

    // ---------------------------------------------------------------------------
    // Behaviour
    // ---------------------------------------------------------------------------

    public void Edit(string body)
    {
        if (IsSoftDeleted)
            throw new InvariantViolationException("I-EDIT-DELETED",
                "A soft-deleted comment may not be edited.");

        ValidateBody(body);

        Body     = body.Trim();
        EditedAt = DateTime.UtcNow;
    }

    public void SoftDelete()
    {
        if (IsSoftDeleted)
            return;

        IsSoftDeleted = true;
        SoftDeletedAt = DateTime.UtcNow;
    }

    public bool IsVisibleTo(Guid requestingUserId, Role requestingUserRole)
    {
        if (Visibility == Visibility.Public)
            return true;

        if (requestingUserRole == Role.Author)
            return true;

        return AuthorId == requestingUserId;
    }

    // ---------------------------------------------------------------------------
    // Private helpers
    // ---------------------------------------------------------------------------

    private static void ValidateBody(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
            throw new InvariantViolationException("I-07",
                "Comment body must not be null or whitespace.");
    }
}
