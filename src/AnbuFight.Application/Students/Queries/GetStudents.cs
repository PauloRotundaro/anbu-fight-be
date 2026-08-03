namespace AnbuFight.Application.Students.Queries;

/// <param name="Status">Filtra por situação: <c>PendingApproval</c> alimenta a fila de aprovação.</param>
public sealed record GetStudentsQuery(
    string? Search = null,
    StudentStatus? Status = null,
    int Page = PagingDefaults.Page,
    int PageSize = PagingDefaults.PageSize) : IRequest<PagedResult<StudentDto>>, IPagedQuery;

public sealed class GetStudentsQueryValidator : AbstractValidator<GetStudentsQuery>
{
    public GetStudentsQueryValidator()
    {
        RuleFor(query => query.Page).GreaterThan(0);
        RuleFor(query => query.PageSize).InclusiveBetween(1, PagingDefaults.MaxPageSize);
        RuleFor(query => query.Search).MaximumLength(100);
        RuleFor(query => query.Status).IsInEnum().When(query => query.Status is not null);
    }
}

public sealed class GetStudentsQueryHandler(IApplicationDbContext context)
    : IRequestHandler<GetStudentsQuery, PagedResult<StudentDto>>
{
    public Task<PagedResult<StudentDto>> Handle(GetStudentsQuery request, CancellationToken cancellationToken)
    {
        var query = context.Students.AsNoTracking();

        if (request.Status is StudentStatus status)
        {
            query = query.Where(student => student.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            // Lowercased on both sides so the comparison stays case-insensitive and provider-agnostic.
            var pattern = $"%{request.Search.Trim().ToLowerInvariant()}%";

            query = query.Where(student =>
                EF.Functions.Like(student.FirstName.ToLower(), pattern) ||
                EF.Functions.Like(student.LastName.ToLower(), pattern) ||
                EF.Functions.Like(student.Email, pattern));
        }

        return query
            .OrderBy(student => student.FirstName)
                .ThenBy(student => student.LastName)
                .ThenBy(student => student.Id)
            .Select(StudentDto.Projection)
            .ToPagedResultAsync(request.Page, request.PageSize, cancellationToken);
    }
}
