using DraftReader.Domain.Enumerations;

namespace DraftReader.Domain.Notifications;

public sealed record CommentNotificationRow(
    Guid          CommentId,
    Guid          SectionId,
    string        SectionTitle,
    Guid          CommentAuthorId,
    string        CommentAuthorName,
    Guid?         ParentCommentId,
    Guid?         ParentCommentAuthorId,
    string        Body,
    DateTime      CreatedAt,
    CommentStatus Status);
