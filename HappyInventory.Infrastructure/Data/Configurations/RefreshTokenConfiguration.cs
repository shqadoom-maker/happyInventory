using HappyInventory.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HappyInventory.Infrastructure.Data.Configurations;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");
        builder.HasKey(token => token.Id);

        builder.Property(token => token.TokenHash)
            .HasMaxLength(64)
            .IsRequired();

        builder.HasIndex(token => token.TokenHash)
            .IsUnique();

        builder.Property(token => token.ReplacedByTokenHash)
            .HasMaxLength(64);

        builder.Property(token => token.CreatedUtc)
            .IsRequired();

        builder.Property(token => token.ExpiresUtc)
            .IsRequired();

        builder.Property(token => token.IsPersistent)
            .IsRequired();

        // A refresh token may only be rotated once. If two requests race with
        // the same token, the second UPDATE sees that RevokedUtc is no longer
        // NULL and fails instead of issuing a second replacement token.
        builder.Property(token => token.RevokedUtc)
            .IsConcurrencyToken();

        builder.HasIndex(token => new { token.UserId, token.RevokedUtc });

        builder.HasOne(token => token.User)
            .WithMany()
            .HasForeignKey(token => token.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
