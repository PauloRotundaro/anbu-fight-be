using AnbuFight.Application.Common.Security;

namespace AnbuFight.Application.Payments.Queries;

public sealed record GetPaymentsQuery(
    Guid? StudentId = null,
    Guid? StudentPlanId = null,
    PaymentStatus? Status = null,
    DateOnly? DueFrom = null,
    DateOnly? DueTo = null,
    int Page = PagingDefaults.Page,
    int PageSize = PagingDefaults.PageSize) : IRequest<PagedResult<PaymentDto>>, IPagedQuery;

public sealed class GetPaymentsQueryValidator : AbstractValidator<GetPaymentsQuery>
{
    public GetPaymentsQueryValidator()
    {
        RuleFor(query => query.Page).GreaterThan(0);
        RuleFor(query => query.PageSize).InclusiveBetween(1, PagingDefaults.MaxPageSize);
        RuleFor(query => query.Status).IsInEnum().When(query => query.Status is not null);

        RuleFor(query => query.DueTo)
            .GreaterThanOrEqualTo(query => query.DueFrom!.Value)
            .When(query => query.DueFrom is not null && query.DueTo is not null)
            .WithMessage("'DueTo' must be on or after 'DueFrom'.");
    }
}

public sealed class GetPaymentsQueryHandler(
    IApplicationDbContext context,
    ICurrentUser currentUser,
    IGymClock clock)
    : IRequestHandler<GetPaymentsQuery, PagedResult<PaymentDto>>
{
    public Task<PagedResult<PaymentDto>> Handle(GetPaymentsQuery request, CancellationToken cancellationToken)
    {
        var studentId = AccessGuard.RestrictStudentFilter(currentUser, request.StudentId);
        var today = clock.Today;

        var query = context.Payments.AsNoTracking();

        if (studentId is Guid student)
        {
            query = query.Where(payment => payment.StudentId == student);
        }

        if (request.StudentPlanId is Guid enrollment)
        {
            query = query.Where(payment => payment.StudentPlanId == enrollment);
        }

        if (request.DueFrom is DateOnly from)
        {
            query = query.Where(payment => payment.DueDate >= from);
        }

        if (request.DueTo is DateOnly to)
        {
            query = query.Where(payment => payment.DueDate <= to);
        }

        query = request.Status switch
        {
            PaymentStatus.Paid => query.Where(payment => payment.PaydAt != null),
            PaymentStatus.Pending => query.Where(payment => payment.PaydAt == null && payment.DueDate >= today),
            PaymentStatus.Overdue => query.Where(payment => payment.PaydAt == null && payment.DueDate < today),
            _ => query
        };

        return query
            .OrderByDescending(payment => payment.DueDate)
                .ThenBy(payment => payment.Id)
            .Select(PaymentDto.Projection(today))
            .ToPagedResultAsync(request.Page, request.PageSize, cancellationToken);
    }
}
