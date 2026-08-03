namespace AnbuFight.Application.Payments.Commands;

/// <summary>
/// Issues a charge against an enrollment. Value and due date default to the enrollment's,
/// so the common case is a single field request. Pass <see cref="PaydAt"/> to record a charge
/// that was already settled at the counter.
/// </summary>
/// <param name="Value">Opcional. Padrão: o valor negociado na matrícula.</param>
/// <param name="DueDate">Opcional. Padrão: o próximo vencimento da matrícula.</param>
/// <param name="PaydAt">Opcional. Informe para registrar uma cobrança já quitada.</param>
public sealed record CreatePaymentCommand(
    Guid StudentPlanId,
    decimal? Value = null,
    DateOnly? DueDate = null,
    DateTimeOffset? PaydAt = null) : IRequest<Guid>;

public sealed class CreatePaymentCommandValidator : AbstractValidator<CreatePaymentCommand>
{
    public CreatePaymentCommandValidator()
    {
        RuleFor(command => command.StudentPlanId).NotEmpty();

        RuleFor(command => command.Value)
            .GreaterThan(0).LessThan(1_000_000)
            .When(command => command.Value is not null);
    }
}

public sealed class CreatePaymentCommandHandler(IApplicationDbContext context)
    : IRequestHandler<CreatePaymentCommand, Guid>
{
    public async Task<Guid> Handle(CreatePaymentCommand request, CancellationToken cancellationToken)
    {
        var enrollment = await context.StudentPlans
            .Include(entity => entity.Plan)
            .FirstOrDefaultAsync(entity => entity.Id == request.StudentPlanId, cancellationToken)
            ?? throw new NotFoundException(nameof(StudentPlan), request.StudentPlanId);

        var payment = new Payment
        {
            StudentId = enrollment.StudentId,
            StudentPlanId = enrollment.Id,
            PlanValue = enrollment.PlanValue,
            Value = request.Value ?? enrollment.PlanValue,
            DueDate = request.DueDate ?? enrollment.DueDate,
            PaydAt = request.PaydAt
        };

        context.Payments.Add(payment);

        if (payment.PaydAt is not null)
        {
            enrollment.AdvanceDueDateFor(payment, enrollment.Plan.Type);
        }

        await context.SaveChangesAsync(cancellationToken);

        return payment.Id;
    }
}
