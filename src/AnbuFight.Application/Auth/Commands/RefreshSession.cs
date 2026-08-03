namespace AnbuFight.Application.Auth.Commands;

/// <summary>
/// Exchanges a refresh token for a new pair. The presented token is always revoked (rotation),
/// so a stolen token is usable at most once.
/// </summary>
public sealed record RefreshSessionCommand(string RefreshToken) : IRequest<AuthenticationResult>;

public sealed class RefreshSessionCommandValidator : AbstractValidator<RefreshSessionCommand>
{
    public RefreshSessionCommandValidator() =>
        RuleFor(command => command.RefreshToken).NotEmpty().MaximumLength(256);
}

public sealed class RefreshSessionCommandHandler(
    IApplicationDbContext context,
    ITokenProvider tokenProvider,
    IGymClock clock)
    : IRequestHandler<RefreshSessionCommand, AuthenticationResult>
{
    public async Task<AuthenticationResult> Handle(
        RefreshSessionCommand request,
        CancellationToken cancellationToken)
    {
        var tokenHash = tokenProvider.HashToken(request.RefreshToken);
        var now = clock.Now;

        var storedToken = await context.RefreshTokens
            .Include(entity => entity.User)
                .ThenInclude(user => user.Student)
            .Include(entity => entity.User)
                .ThenInclude(user => user.Teacher)
            .FirstOrDefaultAsync(entity => entity.TokenHash == tokenHash, cancellationToken);

        if (storedToken is null || !storedToken.IsUsable(now) || !storedToken.User.IsActive)
        {
            throw new AuthenticationFailedException("Invalid or expired refresh token.");
        }

        storedToken.Revoke(now);

        var session = AuthenticationFactory.IssueSession(storedToken.User, context, tokenProvider);

        await context.SaveChangesAsync(cancellationToken);

        return session;
    }
}
