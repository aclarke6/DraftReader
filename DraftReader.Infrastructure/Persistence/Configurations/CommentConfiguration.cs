using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DraftReader.Domain.Entities;

namespace DraftReader.Infrastructure.Persistence.Configurations;

public class CommentConfiguration : IEntityTypeConfiguration<Comment>
{
    public void Configure(EntityTypeBuilder<Comment> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Body)
            .IsRequired()
            .HasColumnType("TEXT");

        builder.Property(c => c.Visibility)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(c => c.CreatedAt)
            .IsRequired();

        builder.Property(c => c.IsSoftDeleted)
            .IsRequired();

        // Self-referential thread
        builder.HasOne<Comment>()
            .WithMany()
            .HasForeignKey(c => c.ParentCommentId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);
    }
}
