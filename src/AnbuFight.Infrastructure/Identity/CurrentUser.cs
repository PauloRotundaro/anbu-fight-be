using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.JsonWebTokens;

namespace AnbuFight.Infrastructure.Identity;

/// <summary>
/// Reads the caller's identity from the validated JWT of the current request.
/// Nothing here hits the database: the token already carries what the handlers need.
/// </summary>
public sealed class CurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    private ClaimsPrincipal? Principal => httpContextAccessor.HttpContext?.User;

    public Guid? UserId => ReadGuid(JwtRegisteredClaimNames.Sub);

    public string? Email => Principal?.FindFirstValue(JwtRegisteredClaimNames.Email);

    public UserRole? Role =>
        Enum.TryParse<UserRole>(Principal?.FindFirstValue(JwtClaims.Role), out var role) ? role : null;

    public Guid? StudentId => ReadGuid(JwtClaims.StudentId);

    public Guid? TeacherId => ReadGuid(JwtClaims.TeacherId);

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated ?? false;

    private Guid? ReadGuid(string claimType) =>
        Guid.TryParse(Principal?.FindFirstValue(claimType), out var value) ? value : null;
}
