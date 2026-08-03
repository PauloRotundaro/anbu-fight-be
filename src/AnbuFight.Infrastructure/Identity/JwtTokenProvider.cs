using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace AnbuFight.Infrastructure.Identity;

/// <summary>
/// Issues the access token (stateless, short lived) and the refresh token (opaque, stored hashed).
/// </summary>
public sealed class JwtTokenProvider : ITokenProvider
{
    private static readonly JsonWebTokenHandler TokenHandler = new();

    private readonly JwtOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly SigningCredentials _signingCredentials;

    public JwtTokenProvider(IOptions<JwtOptions> options, TimeProvider timeProvider)
    {
        _options = options.Value;
        _timeProvider = timeProvider;
        _signingCredentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey)),
            SecurityAlgorithms.HmacSha256);
    }

    public AccessTokenResult CreateAccessToken(User user)
    {
        var issuedAt = _timeProvider.GetUtcNow();
        var expiresAt = issuedAt.AddMinutes(_options.AccessTokenMinutes);

        var claims = new Dictionary<string, object>(5, StringComparer.Ordinal)
        {
            [JwtRegisteredClaimNames.Sub] = user.Id.ToString(),
            [JwtRegisteredClaimNames.Email] = user.Email,
            [JwtRegisteredClaimNames.Jti] = Guid.CreateVersion7().ToString(),
            [JwtClaims.Role] = user.Role.ToString()
        };

        if (user.StudentId is Guid studentId)
        {
            claims[JwtClaims.StudentId] = studentId.ToString();
        }

        if (user.TeacherId is Guid teacherId)
        {
            claims[JwtClaims.TeacherId] = teacherId.ToString();
        }

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            IssuedAt = issuedAt.UtcDateTime,
            NotBefore = issuedAt.UtcDateTime,
            Expires = expiresAt.UtcDateTime,
            SigningCredentials = _signingCredentials,
            Claims = claims
        };

        return new AccessTokenResult(TokenHandler.CreateToken(descriptor), expiresAt);
    }

    public RefreshTokenResult CreateRefreshToken()
    {
        var token = CreateOpaqueToken(TimeSpan.FromDays(_options.RefreshTokenDays));

        return new RefreshTokenResult(token.Value, token.Hash, token.ExpiresAt);
    }

    public OpaqueTokenResult CreateOpaqueToken(TimeSpan lifetime)
    {
        // 256 bits of entropy: not guessable, and never derived from user data.
        var value = Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(32));

        return new OpaqueTokenResult(value, HashToken(value), _timeProvider.GetUtcNow().Add(lifetime));
    }

    public string HashToken(string token) =>
        Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
