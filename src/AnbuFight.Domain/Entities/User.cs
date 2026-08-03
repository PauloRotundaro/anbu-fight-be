using AnbuFight.Domain.Common;
using AnbuFight.Domain.Enums;

namespace AnbuFight.Domain.Entities;

/// <summary>
/// Sign-in credential. Kept apart from <see cref="Student"/>/<see cref="Teacher"/> so those stay
/// pure business entities and so staff accounts can exist without being either of them.
/// </summary>
public class User : BaseEntity
{
    public required string Email { get; set; }

    public required string PasswordHash { get; set; }

    public UserRole Role { get; set; }

    public bool IsActive { get; set; } = true;

    public Guid? StudentId { get; set; }

    public Student? Student { get; set; }

    public Guid? TeacherId { get; set; }

    public Teacher? Teacher { get; set; }

    public ICollection<RefreshToken> RefreshTokens { get; } = [];

    public ICollection<PasswordResetToken> PasswordResetTokens { get; } = [];

    /// <summary>
    /// Encerra as demais sessões após uma troca de senha, preservando opcionalmente a sessão
    /// que originou a troca.
    /// </summary>
    public void RevokeSessions(DateTimeOffset now, string? exceptTokenHash = null)
    {
        foreach (var token in RefreshTokens)
        {
            if (token.RevokedAt is null && token.TokenHash != exceptTokenHash)
            {
                token.Revoke(now);
            }
        }
    }
}
