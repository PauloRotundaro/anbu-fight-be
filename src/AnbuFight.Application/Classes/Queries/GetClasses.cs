namespace AnbuFight.Application.Classes.Queries;

public sealed record GetClassesQuery(
    Modality? Modality = null,
    int? DayOfWeek = null,
    Guid? TeacherId = null,
    bool? IsActive = null,
    int Page = PagingDefaults.Page,
    int PageSize = PagingDefaults.PageSize) : IRequest<PagedResult<ClassDto>>, IPagedQuery;

public sealed class GetClassesQueryValidator : AbstractValidator<GetClassesQuery>
{
    public GetClassesQueryValidator()
    {
        RuleFor(query => query.Page).GreaterThan(0);
        RuleFor(query => query.PageSize).InclusiveBetween(1, PagingDefaults.MaxPageSize);
        RuleFor(query => query.Modality).IsInEnum().When(query => query.Modality is not null);

        RuleFor(query => query.DayOfWeek)
            .InclusiveBetween(0, 6)
            .When(query => query.DayOfWeek is not null);
    }
}

public sealed class GetClassesQueryHandler(IApplicationDbContext context)
    : IRequestHandler<GetClassesQuery, PagedResult<ClassDto>>
{
    public Task<PagedResult<ClassDto>> Handle(GetClassesQuery request, CancellationToken cancellationToken)
    {
        var query = context.Classes.AsNoTracking();

        if (request.Modality is Modality modality)
        {
            query = query.Where(gymClass => gymClass.Modality == modality);
        }

        if (request.TeacherId is Guid teacherId)
        {
            query = query.Where(gymClass => gymClass.TeacherId == teacherId);
        }

        if (request.IsActive is bool isActive)
        {
            query = query.Where(gymClass => gymClass.IsActive == isActive);
        }

        if (request.DayOfWeek is int dayOfWeek)
        {
            query = query.Where(gymClass => gymClass.DaysOfWeek.Contains(dayOfWeek));
        }

        return query
            .OrderBy(gymClass => gymClass.StartTime)
                .ThenBy(gymClass => gymClass.Name)
                .ThenBy(gymClass => gymClass.Id)
            .Select(ClassDto.Projection)
            .ToPagedResultAsync(request.Page, request.PageSize, cancellationToken);
    }
}
