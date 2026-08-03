namespace AnbuFight.Application.Plans.Queries;

public sealed record GetPlansQuery(
    string? Search = null,
    PlanType? Type = null,
    int Page = PagingDefaults.Page,
    int PageSize = PagingDefaults.PageSize) : IRequest<PagedResult<PlanDto>>, IPagedQuery;

public sealed class GetPlansQueryValidator : AbstractValidator<GetPlansQuery>
{
    public GetPlansQueryValidator()
    {
        RuleFor(query => query.Page).GreaterThan(0);
        RuleFor(query => query.PageSize).InclusiveBetween(1, PagingDefaults.MaxPageSize);
        RuleFor(query => query.Search).MaximumLength(120);
        RuleFor(query => query.Type).IsInEnum().When(query => query.Type is not null);
    }
}

public sealed class GetPlansQueryHandler(IApplicationDbContext context)
    : IRequestHandler<GetPlansQuery, PagedResult<PlanDto>>
{
    public Task<PagedResult<PlanDto>> Handle(GetPlansQuery request, CancellationToken cancellationToken)
    {
        var query = context.Plans.AsNoTracking();

        if (request.Type is PlanType type)
        {
            query = query.Where(plan => plan.Type == type);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var pattern = $"%{request.Search.Trim().ToLowerInvariant()}%";
            query = query.Where(plan => EF.Functions.Like(plan.Name.ToLower(), pattern));
        }

        return query
            .OrderBy(plan => plan.Name)
                .ThenBy(plan => plan.Id)
            .Select(PlanDto.Projection)
            .ToPagedResultAsync(request.Page, request.PageSize, cancellationToken);
    }
}
