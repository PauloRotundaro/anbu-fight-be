using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AnbuFight.Infrastructure.Persistence.Configurations;

public sealed class RefreshTokenConfiguration : BaseEntityConfiguration<RefreshToken>
{
    protected override void ConfigureEntity(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("refresh_tokens");

        builder.Property(token => token.TokenHash).HasMaxLength(64).IsRequired();
        builder.Property(token => token.ExpiresAt).IsRequired();

        builder.HasOne(token => token.User)
            .WithMany(user => user.RefreshTokens)
            .HasForeignKey(token => token.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Refresh is a single lookup by hash on every token rotation.
        builder.HasIndex(token => token.TokenHash).IsUnique();

        builder.HasIndex(token => token.UserId);
    }
}
