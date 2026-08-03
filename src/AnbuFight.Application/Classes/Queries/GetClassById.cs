namespace AnbuFight.Application.Classes.Queries;

public sealed record GetClassByIdQuery(Guid Id) : IRequest<ClassDto>;

public sealed class GetClassByIdQueryValidator : AbstractValidator<GetClassByIdQuery>
{
    public GetClassByIdQueryValidator() => RuleFor(query => query.Id).NotEmpty();
}

public sealed class GetClassByIdQueryHandler(IApplicationDbContext context)
    : IRequestHandler<GetClassByIdQuery, ClassDto>
{
    public async Task<ClassDto> Handle(GetClassByIdQuery request, CancellationToken cancellationToken) =>
        await context.Classes
            .AsNoTracking()
            .Where(gymClass => gymClass.Id == request.Id)
            .Select(ClassDto.Projection)
            .FirstOrDefaultAsync(cancellationToken)
        ?? throw new NotFoundException(nameof(Class), request.Id);
}
