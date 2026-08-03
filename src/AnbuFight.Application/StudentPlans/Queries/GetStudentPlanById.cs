using AnbuFight.Application.Common.Security;

namespace AnbuFight.Application.StudentPlans.Queries;

public sealed record GetStudentPlanByIdQuery(Guid Id) : IRequest<StudentPlanDto>;

public sealed class GetStudentPlanByIdQueryValidator : AbstractValidator<GetStudentPlanByIdQuery>
{
    public GetStudentPlanByIdQueryValidator() => RuleFor(query => query.Id).NotEmpty();
}

public sealed class GetStudentPlanByIdQueryHandler(IApplicationDbContext context, ICurrentUser currentUser)
    : IRequestHandler<GetStudentPlanByIdQuery, StudentPlanDto>
{
    public async Task<StudentPlanDto> Handle(
        GetStudentPlanByIdQuery request,
        CancellationToken cancellationToken)
    {
        var enrollment = await context.StudentPlans
            .AsNoTracking()
            .Where(entity => entity.Id == request.Id)
            .Select(StudentPlanDto.Projection)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(nameof(StudentPlan), request.Id);

        AccessGuard.EnsureCanReachStudentData(currentUser, enrollment.StudentId);

        return enrollment;
    }
}
