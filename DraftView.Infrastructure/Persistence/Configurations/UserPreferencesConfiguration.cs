using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DraftView.Domain.Entities;

namespace DraftView.Infrastructure.Persistence.Configurations;

public class UserPreferencesConfiguration : IEntityTypeConfiguration<UserPreferences>
{
    public void Configure(EntityTypeBuilder<UserPreferences> builder)
    {
        builder.HasKey(p => p.Id);

        builder.ToTable("UserPreferences");

        builder.HasIndex(p => p.UserId)
            .IsUnique();

        builder.Property(p => p.DisplayTheme)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(p => p.NotifyOnReply)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(p => p.AuthorDigestMode)
            .HasConversion<string>();

        builder.Property(p => p.AuthorTimezone)
            .HasMaxLength(100);
    }
}
