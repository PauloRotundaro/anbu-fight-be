namespace AnbuFight.Application.Auth.Commands;

public sealed record LoginCommand(string Email, string Password) : IRequest<AuthenticationResult>;

public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(command => command.Email).NotEmpty().MaximumLength(256);
        RuleFor(command => command.Password).NotEmpty().MaximumLength(128);
    }
}

public sealed class LoginCommandHandler(
    IApplicationDbContext context,
    IPasswordHasher passwordHasher,
    ITokenProvider tokenProvider)
    : IRequestHandler<LoginCommand, AuthenticationResult>
{
    public async Task<AuthenticationResult> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        var user = await context.Users
            .Include(entity => entity.Student)
            .Include(entity => entity.Teacher)
            .FirstOrDefaultAsync(entity => entity.Email == email, cancellationToken);

        // The hash is always verified, even for unknown e-mails, to keep the response time flat.
        var passwordMatches = passwordHasher.Verify(
            request.Password,
            user?.PasswordHash ?? passwordHasher.DecoyHash);

        if (user is null || !passwordMatches || !user.IsActive)
        {
            throw new AuthenticationFailedException();
        }

        var session = AuthenticationFactory.IssueSession(user, context, tokenProvider);

        await context.SaveChangesAsync(cancellationToken);

        return session;
    }
}
