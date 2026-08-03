using AnbuFight.Domain.Common;

namespace AnbuFight.Domain.Entities;

/// <summary>
/// Token de redefinição de senha, de uso único. Como o refresh token, só o hash é gravado:
/// um vazamento do banco não permite redefinir a senha de ninguém.
/// </summary>
public class PasswordResetToken : BaseEntity
{
    public Guid UserId { get; set; }

    public User User { get; set; } = null!;

    public required string TokenHash { get; set; }

    public DateTimeOffset ExpiresAt { get; set; }

    public DateTimeOffset? UsedAt { get; set; }

    public bool IsUsable(DateTimeOffset now) => UsedAt is null && ExpiresAt > now;

    public void MarkUsed(DateTimeOffset now) => UsedAt = now;
}
