namespace AnbuFight.Application.Payments.Commands;

public sealed record DeletePaymentCommand(Guid Id) : IRequest;

public sealed class DeletePaymentCommandValidator : AbstractValidator<DeletePaymentCommand>
{
    public DeletePaymentCommandValidator() => RuleFor(command => command.Id).NotEmpty();
}

public sealed class DeletePaymentCommandHandler(IApplicationDbContext context)
    : IRequestHandler<DeletePaymentCommand>
{
    public async Task Handle(DeletePaymentCommand request, CancellationToken cancellationToken)
    {
        var payment = await context.Payments
            .FirstOrDefaultAsync(entity => entity.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Payment), request.Id);

        context.Payments.Remove(payment);

        await context.SaveChangesAsync(cancellationToken);
    }
}
