namespace AnbuFight.Application.StudentPlans.Commands;

/// <summary>Enrolls a student in a plan. A student may hold several plans, but never the same one twice.</summary>
/// <param name="PlanValue">Valor negociado com este aluno; fica congelado nas cobranças já emitidas.</param>
/// <param name="DueDate">Vencimento da primeira cobrança. Avança um ciclo a cada baixa.</param>
public sealed record CreateStudentPlanCommand(
    Guid StudentId,
    Guid PlanId,
    decimal PlanValue,
    DateOnly DueDate) : IRequest<Guid>;

public sealed class CreateStudentPlanCommandValidator : AbstractValidator<CreateStudentPlanCommand>
{
    public CreateStudentPlanCommandValidator()
    {
        RuleFor(command => command.StudentId).NotEmpty();
        RuleFor(command => command.PlanId).NotEmpty();
        RuleFor(command => command.PlanValue).GreaterThan(0).LessThan(1_000_000);
        RuleFor(command => command.DueDate).NotEmpty();
    }
}

public sealed class CreateStudentPlanCommandHandler(IApplicationDbContext context)
    : IRequestHandler<CreateStudentPlanCommand, Guid>
{
    public async Task<Guid> Handle(CreateStudentPlanCommand request, CancellationToken cancellationToken)
    {
        if (!await context.Students.AnyAsync(student => student.Id == request.StudentId, cancellationToken))
        {
            throw new NotFoundException(nameof(Student), request.StudentId);
        }

        if (!await context.Plans.AnyAsync(plan => plan.Id == request.PlanId, cancellationToken))
        {
            throw new NotFoundException(nameof(Plan), request.PlanId);
        }

        var alreadyEnrolled = await context.StudentPlans.AnyAsync(
            enrollment => enrollment.StudentId == request.StudentId && enrollment.PlanId == request.PlanId,
            cancellationToken);

        if (alreadyEnrolled)
        {
            throw new ConflictException("This student is already enrolled in this plan.");
        }

        var studentPlan = new StudentPlan
        {
            StudentId = request.StudentId,
            PlanId = request.PlanId,
            PlanValue = request.PlanValue,
            DueDate = request.DueDate
        };

        context.StudentPlans.Add(studentPlan);
        await context.SaveChangesAsync(cancellationToken);

        return studentPlan.Id;
    }
}
