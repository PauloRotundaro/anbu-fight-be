namespace AnbuFight.Application.Auth;

/// <summary>
/// Builds the token pair that login and refresh both hand back, so the session contract
/// is defined in exactly one place.
/// </summary>
internal static class AuthenticationFactory
{
    public static AuthenticationResult IssueSession(
        User user,
        IApplicationDbContext context,
        ITokenProvider tokenProvider)
    {
        var accessToken = tokenProvider.CreateAccessToken(user);
        var refreshToken = tokenProvider.CreateRefreshToken();

        context.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = refreshToken.Hash,
            ExpiresAt = refreshToken.ExpiresAt
        });

        return new AuthenticationResult(
            accessToken.Value,
            accessToken.ExpiresAt,
            refreshToken.Value,
            refreshToken.ExpiresAt,
            Describe(user));
    }

    public static AuthenticatedUserDto Describe(User user)
    {
        var displayName = user.Student is not null
            ? $"{user.Student.FirstName} {user.Student.LastName}"
            : user.Teacher is not null
                ? $"{user.Teacher.FirstName} {user.Teacher.LastName}"
                : user.Email;

        // Um aluno pendente de aprovação autentica normalmente, mas não é uma conta liberada:
        // o portal usa isso para mostrar a tela de "cadastro em análise".
        var isActive = user.IsActive && (user.Student is null || user.Student.IsActive);

        return new AuthenticatedUserDto(
            user.Id,
            user.Email,
            displayName,
            user.Role,
            isActive,
            user.Student?.Status,
            user.StudentId,
            user.TeacherId);
    }
}
