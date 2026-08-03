namespace AnbuFight.Application.ClassSessions.Queries;

public sealed record GetClassSessionByIdQuery(Guid Id) : IRequest<ClassSessionDto>;

public sealed class GetClassSessionByIdQueryValidator : AbstractValidator<GetClassSessionByIdQuery>
{
    public GetClassSessionByIdQueryValidator() => RuleFor(query => query.Id).NotEmpty();
}

public sealed class GetClassSessionByIdQueryHandler(ClassSessionReader reader)
    : IRequestHandler<GetClassSessionByIdQuery, ClassSessionDto>
{
    public async Task<ClassSessionDto> Handle(
        GetClassSessionByIdQuery request,
        CancellationToken cancellationToken)
    {
        var sessions = await reader.ReadAsync(
            query => query.Where(session => session.Id == request.Id),
            cancellationToken);

        return sessions.FirstOrDefault()
            ?? throw new NotFoundException(nameof(ClassSession), request.Id);
    }
}
