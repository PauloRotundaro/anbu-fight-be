using AnbuFight.Application.UnitTests.Common;
using AnbuFight.Infrastructure.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;

namespace AnbuFight.Application.UnitTests.Identity;

public class JwtTokenProviderTests
{
    private static readonly JwtOptions Options = new()
    {
        Issuer = "AnbuFight",
        Audience = "AnbuFight",
        SigningKey = "unit-tests-signing-key-with-at-least-32-chars",
        AccessTokenMinutes = 15,
        RefreshTokenDays = 14
    };

    private readonly FixedTimeProvider _timeProvider = FixedTimeProvider.Default;
    private readonly JwtTokenProvider _provider;

    public JwtTokenProviderTests() =>
        _provider = new JwtTokenProvider(Microsoft.Extensions.Options.Options.Create(Options), _timeProvider);

    [Fact]
    public void CreateAccessToken_carries_the_identity_the_handlers_rely_on()
    {
        var studentId = Guid.CreateVersion7();
        var user = new User
        {
            Email = "aluno@anbufight.com",
            PasswordHash = "irrelevant",
            Role = UserRole.Student,
            StudentId = studentId
        };

        var accessToken = _provider.CreateAccessToken(user);
        var token = new JsonWebToken(accessToken.Value);

        token.GetClaim(JwtRegisteredClaimNames.Sub).Value.ShouldBe(user.Id.ToString());
        token.GetClaim(JwtRegisteredClaimNames.Email).Value.ShouldBe(user.Email);
        token.GetClaim(JwtClaims.Role).Value.ShouldBe(nameof(UserRole.Student));
        token.GetClaim(JwtClaims.StudentId).Value.ShouldBe(studentId.ToString());
        accessToken.ExpiresAt.ShouldBe(FixedTimeProvider.DefaultNow.AddMinutes(Options.AccessTokenMinutes));
    }

    [Fact]
    public void CreateAccessToken_omits_the_student_claim_for_a_staff_account()
    {
        var user = new User { Email = "admin@anbufight.com", PasswordHash = "irrelevant", Role = UserRole.Admin };

        var token = new JsonWebToken(_provider.CreateAccessToken(user).Value);

        token.TryGetClaim(JwtClaims.StudentId, out _).ShouldBeFalse();
        token.TryGetClaim(JwtClaims.TeacherId, out _).ShouldBeFalse();
    }

    [Fact]
    public void CreateRefreshToken_returns_a_random_value_with_its_hash()
    {
        var first = _provider.CreateRefreshToken();
        var second = _provider.CreateRefreshToken();

        first.Value.ShouldNotBe(second.Value);
        first.Hash.ShouldBe(_provider.HashToken(first.Value));
        first.Hash.ShouldNotBe(first.Value);
        first.ExpiresAt.ShouldBe(FixedTimeProvider.DefaultNow.AddDays(Options.RefreshTokenDays));
    }

    [Fact]
    public void CreateOpaqueToken_honours_the_requested_lifetime()
    {
        var token = _provider.CreateOpaqueToken(TimeSpan.FromHours(2));

        token.ExpiresAt.ShouldBe(FixedTimeProvider.DefaultNow.AddHours(2));
        token.Hash.ShouldBe(_provider.HashToken(token.Value));
    }
}
