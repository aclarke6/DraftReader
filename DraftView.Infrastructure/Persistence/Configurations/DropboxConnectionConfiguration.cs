using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DraftView.Domain.Entities;

namespace DraftView.Infrastructure.Persistence.Configurations;

public class DropboxConnectionConfiguration : IEntityTypeConfiguration<DropboxConnection>
{
    public void Configure(EntityTypeBuilder<DropboxConnection> builder)
    {
        builder.HasKey(d => d.Id);

        builder.Property(d => d.UserId)
            .IsRequired();

        builder.Property(d => d.AccessToken)
            .HasMaxLength(2000);

        builder.Property(d => d.RefreshToken)
            .HasMaxLength(2000);

        builder.Property(d => d.Status)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(d => d.ErrorMessage)
            .HasMaxLength(1000);

        builder.Property(d => d.CreatedAt)
            .IsRequired();

        // One connection per user
        builder.HasIndex(d => d.UserId)
            .IsUnique();

        builder.HasOne<User>()
            .WithOne()
            .HasForeignKey<DropboxConnection>(d => d.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
