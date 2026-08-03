using System.ComponentModel.DataAnnotations;

namespace AnbuFight.Infrastructure.Identity;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    [Required]
    public string Issuer { get; set; } = "AnbuFight";

    [Required]
    public string Audience { get; set; } = "AnbuFight";

    /// <summary>HMAC-SHA256 signing key. Never commit a real key: use user secrets or environment variables.</summary>
    [Required]
    [MinLength(32, ErrorMessage = "The JWT signing key must be at least 32 characters long.")]
    public string SigningKey { get; set; } = string.Empty;

    [Range(1, 1440)]
    public int AccessTokenMinutes { get; set; } = 15;

    [Range(1, 365)]
    public int RefreshTokenDays { get; set; } = 14;
}
