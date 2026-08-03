using AnbuFight.Application.Common.Security;

namespace AnbuFight.Application.StudentPlans.Queries;

public sealed record GetStudentPlansQuery(
    Guid? StudentId = null,
    Guid? PlanId = null,
    int Page = PagingDefaults.Page,
    int PageSize = PagingDefaults.PageSize) : IRequest<PagedResult<StudentPlanDto>>, IPagedQuery;

public sealed class GetStudentPlansQueryValidator : AbstractValidator<GetStudentPlansQuery>
{
    public GetStudentPlansQueryValidator()
    {
        RuleFor(query => query.Page).GreaterThan(0);
        RuleFor(query => query.PageSize).InclusiveBetween(1, PagingDefaults.MaxPageSize);
    }
}

public sealed class GetStudentPlansQueryHandler(IApplicationDbContext context, ICurrentUser currentUser)
    : IRequestHandler<GetStudentPlansQuery, PagedResult<StudentPlanDto>>
{
    public Task<PagedResult<StudentPlanDto>> Handle(
        GetStudentPlansQuery request,
        CancellationToken cancellationToken)
    {
        var studentId = AccessGuard.RestrictStudentFilter(currentUser, request.StudentId);

        var query = context.StudentPlans.AsNoTracking();

        if (studentId is Guid student)
        {
            query = query.Where(enrollment => enrollment.StudentId == student);
        }

        if (request.PlanId is Guid plan)
        {
            query = query.Where(enrollment => enrollment.PlanId == plan);
        }

        return query
            .OrderBy(enrollment => enrollment.DueDate)
                .ThenBy(enrollment => enrollment.Id)
            .Select(StudentPlanDto.Projection)
            .ToPagedResultAsync(request.Page, request.PageSize, cancellationToken);
    }
}
