namespace AnbuFight.Application.Plans.Commands;

public sealed record DeletePlanCommand(Guid Id) : IRequest;

public sealed class DeletePlanCommandValidator : AbstractValidator<DeletePlanCommand>
{
    public DeletePlanCommandValidator() => RuleFor(command => command.Id).NotEmpty();
}

public sealed class DeletePlanCommandHandler(IApplicationDbContext context) : IRequestHandler<DeletePlanCommand>
{
    public async Task Handle(DeletePlanCommand request, CancellationToken cancellationToken)
    {
        var plan = await context.Plans
            .FirstOrDefaultAsync(entity => entity.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Plan), request.Id);

        // Removing a plan that students are still enrolled in would silently break their billing.
        if (await context.StudentPlans.AnyAsync(
                enrollment => enrollment.PlanId == plan.Id, cancellationToken))
        {
            throw new ConflictException(
                "This plan still has active enrollments. Remove the enrollments before deleting the plan.");
        }

        context.Plans.Remove(plan);

        await context.SaveChangesAsync(cancellationToken);
    }
}
