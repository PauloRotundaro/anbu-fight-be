namespace AnbuFight.Application.StudentPlans.Commands;

public sealed record DeleteStudentPlanCommand(Guid Id) : IRequest;

public sealed class DeleteStudentPlanCommandValidator : AbstractValidator<DeleteStudentPlanCommand>
{
    public DeleteStudentPlanCommandValidator() => RuleFor(command => command.Id).NotEmpty();
}

public sealed class DeleteStudentPlanCommandHandler(IApplicationDbContext context)
    : IRequestHandler<DeleteStudentPlanCommand>
{
    public async Task Handle(DeleteStudentPlanCommand request, CancellationToken cancellationToken)
    {
        var enrollment = await context.StudentPlans
            .FirstOrDefaultAsync(entity => entity.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(StudentPlan), request.Id);

        // Open charges must be settled or cancelled first, otherwise they would be orphaned.
        var hasOpenPayments = await context.Payments.AnyAsync(
            payment => payment.StudentPlanId == enrollment.Id && payment.PaydAt == null,
            cancellationToken);

        if (hasOpenPayments)
        {
            throw new ConflictException(
                "This enrollment still has open payments. Settle or delete them before removing it.");
        }

        context.StudentPlans.Remove(enrollment);

        await context.SaveChangesAsync(cancellationToken);
    }
}
