using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DraftView.Domain.Entities;

namespace DraftView.Infrastructure.Persistence.Configurations;

public class AuthorNotificationConfiguration : IEntityTypeConfiguration<AuthorNotification>
{
    public void Configure(EntityTypeBuilder<AuthorNotification> builder)
    {
        builder.HasKey(n => n.Id);
        builder.Property(n => n.AuthorId).IsRequired();
        builder.Property(n => n.EventType).IsRequired().HasConversion<string>();
        builder.Property(n => n.Title).IsRequired().HasMaxLength(300);
        builder.Property(n => n.Detail).HasMaxLength(500);
        builder.Property(n => n.LinkUrl).HasMaxLength(500);
        builder.Property(n => n.OccurredAt).IsRequired();
        builder.HasIndex(n => new { n.AuthorId, n.OccurredAt });
    }
}
