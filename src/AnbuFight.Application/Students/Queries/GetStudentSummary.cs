using AnbuFight.Application.Attendances;
using AnbuFight.Application.Attendances.Queries;
using AnbuFight.Application.Common.Security;
using AnbuFight.Application.Payments;
using AnbuFight.Application.StudentPlans;

namespace AnbuFight.Application.Students.Queries;

/// <summary>
/// Tudo o que a tela de detalhe do aluno mostra, numa resposta só — em vez de quatro requisições
/// em cascata que fariam a tela carregar em etapas.
/// </summary>
public sealed record StudentSummaryDto(
    StudentDto Student,
    IReadOnlyList<StudentPlanDto> Enrollments,
    IReadOnlyList<PaymentDto> RecentPayments,
    IReadOnlyList<AttendanceDto> RecentAttendances,
    CheckInEligibilityDto Eligibility);

public sealed record GetStudentSummaryQuery(Guid Id, int RecentItems = 5) : IRequest<StudentSummaryDto>;

public sealed class GetStudentSummaryQueryValidator : AbstractValidator<GetStudentSummaryQuery>
{
    public GetStudentSummaryQueryValidator()
    {
        RuleFor(query => query.Id).NotEmpty();
        RuleFor(query => query.RecentItems).InclusiveBetween(1, 50);
    }
}

public sealed class GetStudentSummaryQueryHandler(
    IApplicationDbContext context,
    ICurrentUser currentUser,
    IGymClock clock,
    CheckInPolicy checkInPolicy)
    : IRequestHandler<GetStudentSummaryQuery, StudentSummaryDto>
{
    public async Task<StudentSummaryDto> Handle(
        GetStudentSummaryQuery request,
        CancellationToken cancellationToken)
    {
        AccessGuard.EnsureCanReachStudentData(currentUser, request.Id);

        var student = await context.Students
            .AsNoTracking()
            .Where(entity => entity.Id == request.Id)
            .Select(StudentDto.Projection)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(nameof(Student), request.Id);

        var today = clock.Today;

        var enrollments = await context.StudentPlans
            .AsNoTracking()
            .Where(enrollment => enrollment.StudentId == request.Id)
            .OrderBy(enrollment => enrollment.DueDate)
            .Select(StudentPlanDto.Projection)
            .ToListAsync(cancellationToken);

        var recentPayments = await context.Payments
            .AsNoTracking()
            .Where(payment => payment.StudentId == request.Id)
            .OrderByDescending(payment => payment.DueDate)
                .ThenBy(payment => payment.Id)
            .Take(request.RecentItems)
            .Select(PaymentDto.Projection(today))
            .ToListAsync(cancellationToken);

        var recentAttendances = await context.Attendances
            .AsNoTracking()
            .Where(attendance => attendance.StudentId == request.Id)
            .OrderByDescending(attendance => attendance.CheckedInAt)
            .Take(request.RecentItems)
            .Select(AttendanceProjections.ToDto)
            .ToListAsync(cancellationToken);

        var eligibility = await checkInPolicy.EvaluateAsync(request.Id, cancellationToken);

        return new StudentSummaryDto(student, enrollments, recentPayments, recentAttendances, eligibility);
    }
}
