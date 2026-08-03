namespace AnbuFight.Application.Auth.Commands;

/// <summary>
/// Consome o token recebido por e-mail e grava a nova senha. O token é de uso único e todas as
/// sessões ativas caem — quem pediu a redefinição provavelmente perdeu o controle da conta.
/// </summary>
public sealed record ResetPasswordCommand(string Token, string NewPassword) : IRequest;

public sealed class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordCommandValidator()
    {
        RuleFor(command => command.Token).NotEmpty().MaximumLength(256);
        RuleFor(command => command.NewPassword).RequiredPassword();
    }
}

public sealed class ResetPasswordCommandHandler(
    IApplicationDbContext context,
    IPasswordHasher passwordHasher,
    ITokenProvider tokenProvider,
    IGymClock clock)
    : IRequestHandler<ResetPasswordCommand>
{
    public async Task Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        var tokenHash = tokenProvider.HashToken(request.Token);
        var now = clock.Now;

        var resetToken = await context.PasswordResetTokens
            .Include(entity => entity.User)
                .ThenInclude(user => user.RefreshTokens)
            .FirstOrDefaultAsync(entity => entity.TokenHash == tokenHash, cancellationToken);

        if (resetToken is null || !resetToken.IsUsable(now))
        {
            throw new ValidationException([
                new FluentValidation.Results.ValidationFailure(
                    nameof(ResetPasswordCommand.Token),
                    "O link de redefinição é inválido ou expirou. Peça um novo.")
            ]);
        }

        resetToken.MarkUsed(now);
        resetToken.User.PasswordHash = passwordHasher.Hash(request.NewPassword);
        resetToken.User.RevokeSessions(now);

        await context.SaveChangesAsync(cancellationToken);
    }
}
