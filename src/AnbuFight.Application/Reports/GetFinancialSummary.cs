namespace AnbuFight.Application.Reports;

/// <param name="BilledTotal">Soma das cobranças com vencimento no período.</param>
/// <param name="ReceivedTotal">Soma das cobranças quitadas no período, pela data de pagamento.</param>
/// <param name="PendingTotal">Em aberto e ainda dentro do prazo.</param>
/// <param name="OverdueTotal">Em aberto e vencido.</param>
/// <param name="DefaultRate">Inadimplência: vencido dividido pelo faturado. 0 quando não há faturamento.</param>
public sealed record FinancialSummaryDto(
    DateOnly From,
    DateOnly To,
    decimal BilledTotal,
    decimal ReceivedTotal,
    decimal PendingTotal,
    decimal OverdueTotal,
    int OverdueCount,
    int ActiveStudents,
    int NewEnrollments,
    decimal DefaultRate,
    IReadOnlyList<MonthlyRevenue> RevenueByMonth);

/// <param name="Month">Competência no formato <c>YYYY-MM</c>.</param>
public sealed record MonthlyRevenue(string Month, decimal Billed, decimal Received);

/// <summary>
/// Números do dashboard da gestão. Agregado no banco: somar isso no cliente exigiria paginar
/// a tabela inteira de cobranças a cada carregamento da tela.
/// </summary>
public sealed record GetFinancialSummaryQuery(DateOnly? From = null, DateOnly? To = null)
    : IRequest<FinancialSummaryDto>;

public sealed class GetFinancialSummaryQueryValidator : AbstractValidator<GetFinancialSummaryQuery>
{
    public GetFinancialSummaryQueryValidator() =>
        RuleFor(query => query.To)
            .GreaterThanOrEqualTo(query => query.From!.Value)
            .When(query => query.From is not null && query.To is not null)
            .WithMessage("'To' deve ser igual ou posterior a 'From'.");
}

public sealed class GetFinancialSummaryQueryHandler(IApplicationDbContext context, IGymClock clock)
    : IRequestHandler<GetFinancialSummaryQuery, FinancialSummaryDto>
{
    public async Task<FinancialSummaryDto> Handle(
        GetFinancialSummaryQuery request,
        CancellationToken cancellationToken)
    {
        var today = clock.Today;
        var from = request.From ?? new DateOnly(today.Year, today.Month, 1);
        var to = request.To ?? from.AddMonths(1).AddDays(-1);

        var billed = await context.Payments
            .AsNoTracking()
            .Where(payment => payment.DueDate >= from && payment.DueDate <= to)
            .GroupBy(_ => 1)
            .Select(group => new
            {
                Total = group.Sum(payment => payment.Value),
                Pending = group
                    .Where(payment => payment.PaydAt == null && payment.DueDate >= today)
                    .Sum(payment => payment.Value),
                Overdue = group
                    .Where(payment => payment.PaydAt == null && payment.DueDate < today)
                    .Sum(payment => payment.Value),
                OverdueCount = group.Count(payment => payment.PaydAt == null && payment.DueDate < today)
            })
            .FirstOrDefaultAsync(cancellationToken);

        var fromInstant = clock.ToInstant(from, TimeOnly.MinValue);
        var toInstant = clock.ToInstant(to.AddDays(1), TimeOnly.MinValue);

        var received = await context.Payments
            .AsNoTracking()
            .Where(payment => payment.PaydAt >= fromInstant && payment.PaydAt < toInstant)
            .SumAsync(payment => (decimal?)payment.Value, cancellationToken) ?? 0m;

        var activeStudents = await context.Students
            .CountAsync(student => student.Status == StudentStatus.Active, cancellationToken);

        var newEnrollments = await context.StudentPlans
            .CountAsync(
                enrollment => enrollment.CreatedAt >= fromInstant && enrollment.CreatedAt < toInstant,
                cancellationToken);

        var revenueByMonth = await BuildMonthlySeriesAsync(from, to, fromInstant, toInstant, cancellationToken);

        var billedTotal = billed?.Total ?? 0m;

        return new FinancialSummaryDto(
            from,
            to,
            billedTotal,
            received,
            billed?.Pending ?? 0m,
            billed?.Overdue ?? 0m,
            billed?.OverdueCount ?? 0,
            activeStudents,
            newEnrollments,
            billedTotal == 0m ? 0m : Math.Round((billed?.Overdue ?? 0m) / billedTotal, 4),
            revenueByMonth);
    }

    private async Task<List<MonthlyRevenue>> BuildMonthlySeriesAsync(
        DateOnly from,
        DateOnly to,
        DateTimeOffset fromInstant,
        DateTimeOffset toInstant,
        CancellationToken cancellationToken)
    {
        var billedByMonth = await context.Payments
            .AsNoTracking()
            .Where(payment => payment.DueDate >= from && payment.DueDate <= to)
            .GroupBy(payment => new { payment.DueDate.Year, payment.DueDate.Month })
            .Select(group => new
            {
                group.Key.Year,
                group.Key.Month,
                Total = group.Sum(payment => payment.Value)
            })
            .ToListAsync(cancellationToken);

        var receivedByMonth = await context.Payments
            .AsNoTracking()
            .Where(payment => payment.PaydAt >= fromInstant && payment.PaydAt < toInstant)
            .GroupBy(payment => new { payment.PaydAt!.Value.Year, payment.PaydAt!.Value.Month })
            .Select(group => new
            {
                group.Key.Year,
                group.Key.Month,
                Total = group.Sum(payment => payment.Value)
            })
            .ToListAsync(cancellationToken);

        var months = new List<MonthlyRevenue>();

        for (var month = new DateOnly(from.Year, from.Month, 1);
             month <= to;
             month = month.AddMonths(1))
        {
            var billed = billedByMonth
                .FirstOrDefault(entry => entry.Year == month.Year && entry.Month == month.Month)?.Total ?? 0m;

            var received = receivedByMonth
                .FirstOrDefault(entry => entry.Year == month.Year && entry.Month == month.Month)?.Total ?? 0m;

            months.Add(new MonthlyRevenue($"{month.Year:D4}-{month.Month:D2}", billed, received));
        }

        return months;
    }
}
