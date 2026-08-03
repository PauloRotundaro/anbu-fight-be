namespace AnbuFight.Application.Plans.Queries;

public sealed record GetPlanByIdQuery(Guid Id) : IRequest<PlanDto>;

public sealed class GetPlanByIdQueryValidator : AbstractValidator<GetPlanByIdQuery>
{
    public GetPlanByIdQueryValidator() => RuleFor(query => query.Id).NotEmpty();
}

public sealed class GetPlanByIdQueryHandler(IApplicationDbContext context)
    : IRequestHandler<GetPlanByIdQuery, PlanDto>
{
    public async Task<PlanDto> Handle(GetPlanByIdQuery request, CancellationToken cancellationToken) =>
        await context.Plans
            .AsNoTracking()
            .Where(plan => plan.Id == request.Id)
            .Select(PlanDto.Projection)
            .FirstOrDefaultAsync(cancellationToken)
        ?? throw new NotFoundException(nameof(Plan), request.Id);
}
