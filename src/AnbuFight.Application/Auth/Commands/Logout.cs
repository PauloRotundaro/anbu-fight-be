namespace AnbuFight.Application.Auth.Commands;

/// <summary>
/// Revokes the presented refresh token, or every session of the caller when none is given.
/// </summary>
public sealed record LogoutCommand(string? RefreshToken = null) : IRequest;

public sealed class LogoutCommandHandler(
    IApplicationDbContext context,
    ICurrentUser currentUser,
    ITokenProvider tokenProvider,
    IGymClock clock)
    : IRequestHandler<LogoutCommand>
{
    public async Task Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not Guid userId)
        {
            throw new AuthenticationFailedException();
        }

        var now = clock.Now;

        var query = context.RefreshTokens.Where(token => token.UserId == userId && token.RevokedAt == null);

        if (!string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            var tokenHash = tokenProvider.HashToken(request.RefreshToken);
            query = query.Where(token => token.TokenHash == tokenHash);
        }

        foreach (var token in await query.ToListAsync(cancellationToken))
        {
            token.Revoke(now);
        }

        await context.SaveChangesAsync(cancellationToken);
    }
}
