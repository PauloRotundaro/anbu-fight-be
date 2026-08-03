namespace AnbuFight.Application.Teachers.Queries;

public sealed record GetTeachersQuery(
    string? Search = null,
    int Page = PagingDefaults.Page,
    int PageSize = PagingDefaults.PageSize) : IRequest<PagedResult<TeacherDto>>, IPagedQuery;

public sealed class GetTeachersQueryValidator : AbstractValidator<GetTeachersQuery>
{
    public GetTeachersQueryValidator()
    {
        RuleFor(query => query.Page).GreaterThan(0);
        RuleFor(query => query.PageSize).InclusiveBetween(1, PagingDefaults.MaxPageSize);
        RuleFor(query => query.Search).MaximumLength(100);
    }
}

public sealed class GetTeachersQueryHandler(IApplicationDbContext context)
    : IRequestHandler<GetTeachersQuery, PagedResult<TeacherDto>>
{
    public Task<PagedResult<TeacherDto>> Handle(GetTeachersQuery request, CancellationToken cancellationToken)
    {
        var query = context.Teachers.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var pattern = $"%{request.Search.Trim().ToLowerInvariant()}%";

            query = query.Where(teacher =>
                EF.Functions.Like(teacher.FirstName.ToLower(), pattern) ||
                EF.Functions.Like(teacher.LastName.ToLower(), pattern) ||
                EF.Functions.Like(teacher.Email, pattern));
        }

        return query
            .OrderBy(teacher => teacher.FirstName)
                .ThenBy(teacher => teacher.LastName)
                .ThenBy(teacher => teacher.Id)
            .Select(TeacherDto.Projection)
            .ToPagedResultAsync(request.Page, request.PageSize, cancellationToken);
    }
}
