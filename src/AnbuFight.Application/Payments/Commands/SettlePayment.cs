namespace AnbuFight.Application.Payments.Commands;

/// <summary>
/// Marks a charge as paid and rolls the enrollment to the next billing cycle.
/// </summary>
public sealed record SettlePaymentCommand(Guid Id, DateTimeOffset? PaydAt = null) : IRequest;

public sealed class SettlePaymentCommandValidator : AbstractValidator<SettlePaymentCommand>
{
    public SettlePaymentCommandValidator() => RuleFor(command => command.Id).NotEmpty();
}

public sealed class SettlePaymentCommandHandler(IApplicationDbContext context, IGymClock clock)
    : IRequestHandler<SettlePaymentCommand>
{
    public async Task Handle(SettlePaymentCommand request, CancellationToken cancellationToken)
    {
        var payment = await context.Payments
            .Include(entity => entity.StudentPlan)
                .ThenInclude(enrollment => enrollment.Plan)
            .FirstOrDefaultAsync(entity => entity.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Payment), request.Id);

        if (payment.PaydAt is not null)
        {
            throw new ConflictException("This payment has already been settled.");
        }

        payment.Settle(request.PaydAt ?? clock.Now);
        payment.StudentPlan.AdvanceDueDateFor(payment, payment.StudentPlan.Plan.Type);

        await context.SaveChangesAsync(cancellationToken);
    }
}
