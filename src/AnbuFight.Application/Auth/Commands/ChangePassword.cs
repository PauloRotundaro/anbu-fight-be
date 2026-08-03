namespace AnbuFight.Application.Auth.Commands;

/// <summary>
/// Troca a própria senha. As demais sessões são encerradas — se a senha vazou, trocar a senha
/// tem de derrubar quem estiver usando a conta.
/// </summary>
/// <param name="CurrentRefreshToken">
/// Opcional. Informe o refresh token da sessão atual para mantê-la viva; sem ele, todas as
/// sessões caem e o usuário precisa entrar de novo.
/// </param>
public sealed record ChangePasswordCommand(
    string CurrentPassword,
    string NewPassword,
    string? CurrentRefreshToken = null) : IRequest;

public sealed class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordCommandValidator()
    {
        RuleFor(command => command.CurrentPassword).NotEmpty();
        RuleFor(command => command.NewPassword).RequiredPassword();

        RuleFor(command => command.NewPassword)
            .NotEqual(command => command.CurrentPassword)
            .WithMessage("A nova senha deve ser diferente da atual.");
    }
}

public sealed class ChangePasswordCommandHandler(
    IApplicationDbContext context,
    ICurrentUser currentUser,
    IPasswordHasher passwordHasher,
    ITokenProvider tokenProvider,
    IGymClock clock)
    : IRequestHandler<ChangePasswordCommand>
{
    public async Task Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not Guid userId)
        {
            throw new AuthenticationFailedException();
        }

        var user = await context.Users
            .Include(entity => entity.RefreshTokens)
            .FirstOrDefaultAsync(entity => entity.Id == userId, cancellationToken)
            ?? throw new AuthenticationFailedException();

        if (!passwordHasher.Verify(request.CurrentPassword, user.PasswordHash))
        {
            throw new AuthenticationFailedException("A senha atual está incorreta.");
        }

        user.PasswordHash = passwordHasher.Hash(request.NewPassword);

        var keptSession = string.IsNullOrWhiteSpace(request.CurrentRefreshToken)
            ? null
            : tokenProvider.HashToken(request.CurrentRefreshToken);

        user.RevokeSessions(clock.Now, keptSession);

        await context.SaveChangesAsync(cancellationToken);
    }
}
