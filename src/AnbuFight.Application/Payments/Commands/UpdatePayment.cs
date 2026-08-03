namespace AnbuFight.Application.Payments.Commands;

/// <summary>Corrects an open charge. Settled charges are immutable — use a new charge instead.</summary>
public sealed record UpdatePaymentCommand(Guid Id, decimal Value, DateOnly DueDate) : IRequest;

public sealed class UpdatePaymentCommandValidator : AbstractValidator<UpdatePaymentCommand>
{
    public UpdatePaymentCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty();
        RuleFor(command => command.Value).GreaterThan(0).LessThan(1_000_000);
        RuleFor(command => command.DueDate).NotEmpty();
    }
}

public sealed class UpdatePaymentCommandHandler(IApplicationDbContext context)
    : IRequestHandler<UpdatePaymentCommand>
{
    public async Task Handle(UpdatePaymentCommand request, CancellationToken cancellationToken)
    {
        var payment = await context.Payments
            .FirstOrDefaultAsync(entity => entity.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Payment), request.Id);

        if (payment.PaydAt is not null)
        {
            throw new ConflictException("A settled payment cannot be changed.");
        }

        payment.Value = request.Value;
        payment.DueDate = request.DueDate;

        await context.SaveChangesAsync(cancellationToken);
    }
}
