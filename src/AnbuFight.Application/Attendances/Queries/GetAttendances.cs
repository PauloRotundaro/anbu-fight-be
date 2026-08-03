using AnbuFight.Application.Common.Security;

namespace AnbuFight.Application.Attendances.Queries;

public sealed record GetAttendancesQuery(
    Guid? StudentId = null,
    Guid? ClassSessionId = null,
    Modality? Modality = null,
    DateOnly? From = null,
    DateOnly? To = null,
    int Page = PagingDefaults.Page,
    int PageSize = PagingDefaults.PageSize) : IRequest<PagedResult<AttendanceDto>>, IPagedQuery;

public sealed class GetAttendancesQueryValidator : AbstractValidator<GetAttendancesQuery>
{
    public GetAttendancesQueryValidator()
    {
        RuleFor(query => query.Page).GreaterThan(0);
        RuleFor(query => query.PageSize).InclusiveBetween(1, PagingDefaults.MaxPageSize);
        RuleFor(query => query.Modality).IsInEnum().When(query => query.Modality is not null);

        RuleFor(query => query.To)
            .GreaterThanOrEqualTo(query => query.From!.Value)
            .When(query => query.From is not null && query.To is not null)
            .WithMessage("'To' deve ser igual ou posterior a 'From'.");
    }
}

public sealed class GetAttendancesQueryHandler(IApplicationDbContext context, ICurrentUser currentUser)
    : IRequestHandler<GetAttendancesQuery, PagedResult<AttendanceDto>>
{
    public Task<PagedResult<AttendanceDto>> Handle(
        GetAttendancesQuery request,
        CancellationToken cancellationToken)
    {
        var studentId = AccessGuard.RestrictStudentFilter(currentUser, request.StudentId);

        var query = context.Attendances.AsNoTracking();

        if (studentId is Guid student)
        {
            query = query.Where(attendance => attendance.StudentId == student);
        }

        if (request.ClassSessionId is Guid session)
        {
            query = query.Where(attendance => attendance.ClassSessionId == session);
        }

        if (request.Modality is Modality modality)
        {
            query = query.Where(attendance => attendance.ClassSession.Class.Modality == modality);
        }

        if (request.From is DateOnly from)
        {
            query = query.Where(attendance => attendance.ClassSession.Date >= from);
        }

        if (request.To is DateOnly to)
        {
            query = query.Where(attendance => attendance.ClassSession.Date <= to);
        }

        return query
            .OrderByDescending(attendance => attendance.CheckedInAt)
                .ThenBy(attendance => attendance.Id)
            .Select(AttendanceProjections.ToDto)
            .ToPagedResultAsync(request.Page, request.PageSize, cancellationToken);
    }
}
