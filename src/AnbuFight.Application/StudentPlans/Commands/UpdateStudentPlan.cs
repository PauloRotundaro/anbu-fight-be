namespace AnbuFight.Application.StudentPlans.Commands;

/// <summary>
/// Only the negotiated price and the next due date can change. Moving an enrollment to another
/// student or plan would rewrite the history of every payment already issued against it.
/// </summary>
public sealed record UpdateStudentPlanCommand(Guid Id, decimal PlanValue, DateOnly DueDate) : IRequest;

public sealed class UpdateStudentPlanCommandValidator : AbstractValidator<UpdateStudentPlanCommand>
{
    public UpdateStudentPlanCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty();
        RuleFor(command => command.PlanValue).GreaterThan(0).LessThan(1_000_000);
        RuleFor(command => command.DueDate).NotEmpty();
    }
}

public sealed class UpdateStudentPlanCommandHandler(IApplicationDbContext context)
    : IRequestHandler<UpdateStudentPlanCommand>
{
    public async Task Handle(UpdateStudentPlanCommand request, CancellationToken cancellationToken)
    {
        var enrollment = await context.StudentPlans
            .FirstOrDefaultAsync(entity => entity.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(StudentPlan), request.Id);

        enrollment.PlanValue = request.PlanValue;
        enrollment.DueDate = request.DueDate;

        await context.SaveChangesAsync(cancellationToken);
    }
}
