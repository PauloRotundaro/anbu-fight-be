using AnbuFight.Application.Common.Security;

namespace AnbuFight.Application.Payments.Queries;

public sealed record GetPaymentByIdQuery(Guid Id) : IRequest<PaymentDto>;

public sealed class GetPaymentByIdQueryValidator : AbstractValidator<GetPaymentByIdQuery>
{
    public GetPaymentByIdQueryValidator() => RuleFor(query => query.Id).NotEmpty();
}

public sealed class GetPaymentByIdQueryHandler(
    IApplicationDbContext context,
    ICurrentUser currentUser,
    IGymClock clock)
    : IRequestHandler<GetPaymentByIdQuery, PaymentDto>
{
    public async Task<PaymentDto> Handle(GetPaymentByIdQuery request, CancellationToken cancellationToken)
    {
        var today = clock.Today;

        var payment = await context.Payments
            .AsNoTracking()
            .Where(entity => entity.Id == request.Id)
            .Select(PaymentDto.Projection(today))
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(nameof(Payment), request.Id);

        AccessGuard.EnsureCanReachStudentData(currentUser, payment.StudentId);

        return payment;
    }
}
