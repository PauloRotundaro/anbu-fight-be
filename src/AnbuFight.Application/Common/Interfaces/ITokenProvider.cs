using AnbuFight.Domain.Entities;

namespace AnbuFight.Application.Common.Interfaces;

public interface ITokenProvider
{
    AccessTokenResult CreateAccessToken(User user);

    RefreshTokenResult CreateRefreshToken();

    /// <summary>Token opaco de uso único, para redefinição de senha.</summary>
    OpaqueTokenResult CreateOpaqueToken(TimeSpan lifetime);

    /// <summary>Hash usado para localizar um token opaco apresentado pelo cliente.</summary>
    string HashToken(string token);
}

public readonly record struct AccessTokenResult(string Value, DateTimeOffset ExpiresAt);

public readonly record struct RefreshTokenResult(string Value, string Hash, DateTimeOffset ExpiresAt);

public readonly record struct OpaqueTokenResult(string Value, string Hash, DateTimeOffset ExpiresAt);
