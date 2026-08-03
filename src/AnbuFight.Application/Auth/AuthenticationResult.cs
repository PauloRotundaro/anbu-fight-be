namespace AnbuFight.Application.Auth;

/// <param name="AccessToken">JWT a ser enviado no cabeçalho <c>Authorization: Bearer</c>.</param>
/// <param name="ExpiresAt">Expiração do access token.</param>
/// <param name="RefreshToken">Token opaco para obter um novo par em <c>POST /api/auth/refresh</c>.</param>
public sealed record AuthenticationResult(
    string AccessToken,
    DateTimeOffset ExpiresAt,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt,
    AuthenticatedUserDto User);

/// <param name="IsActive">
/// Se a conta está plenamente liberada. Falso para aluno pendente de aprovação ou inativo —
/// é o que o portal usa para mostrar a tela de "cadastro em análise".
/// </param>
/// <param name="StudentStatus">Situação do aluno; nulo para contas que não são de aluno.</param>
/// <param name="StudentId">Preenchido quando a credencial pertence a um aluno.</param>
/// <param name="TeacherId">Preenchido quando a credencial pertence a um professor.</param>
public sealed record AuthenticatedUserDto(
    Guid Id,
    string Email,
    string DisplayName,
    UserRole Role,
    bool IsActive,
    StudentStatus? StudentStatus,
    Guid? StudentId,
    Guid? TeacherId);
