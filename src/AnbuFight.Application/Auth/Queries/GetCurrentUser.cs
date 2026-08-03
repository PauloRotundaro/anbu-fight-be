namespace AnbuFight.Application.Auth.Queries;

/// <summary>Profile of the caller, resolved from the JWT. Used by clients to bootstrap the UI.</summary>
public sealed record GetCurrentUserQuery : IRequest<AuthenticatedUserDto>;

public sealed class GetCurrentUserQueryHandler(IApplicationDbContext context, ICurrentUser currentUser)
    : IRequestHandler<GetCurrentUserQuery, AuthenticatedUserDto>
{
    public async Task<AuthenticatedUserDto> Handle(
        GetCurrentUserQuery request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not Guid userId)
        {
            throw new AuthenticationFailedException();
        }

        var user = await context.Users
            .AsNoTracking()
            .Include(entity => entity.Student)
            .Include(entity => entity.Teacher)
            .FirstOrDefaultAsync(entity => entity.Id == userId, cancellationToken)
            ?? throw new AuthenticationFailedException();

        return AuthenticationFactory.Describe(user);
    }
}
