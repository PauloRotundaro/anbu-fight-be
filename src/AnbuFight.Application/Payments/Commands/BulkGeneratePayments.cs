using System.Globalization;

namespace AnbuFight.Application.Payments.Commands;

/// <param name="Month">Competência no formato <c>YYYY-MM</c>.</param>
/// <param name="Created">Quantas cobranças foram emitidas.</param>
/// <param name="Skipped">Quantas matrículas já tinham cobrança para aquele vencimento.</param>
public sealed record BulkGenerationResult(string Month, int Created, int Skipped);

/// <summary>
/// Emite de uma vez as cobranças do mês para todas as matrículas com vencimento no período.
/// É idempotente: rodar duas vezes não duplica nada, apenas aumenta o número de ignoradas.
/// </summary>
public sealed record BulkGeneratePaymentsCommand(
    string Month,
    Guid? PlanId = null,
    Guid? StudentId = null) : IRequest<BulkGenerationResult>;

public sealed class BulkGeneratePaymentsCommandValidator : AbstractValidator<BulkGeneratePaymentsCommand>
{
    public BulkGeneratePaymentsCommandValidator() =>
        RuleFor(command => command.Month)
            .NotEmpty()
            .Must(month => TryParseMonth(month, out _))
            .WithMessage("Informe a competência no formato AAAA-MM.");

    internal static bool TryParseMonth(string? month, out DateOnly firstDay) =>
        DateOnly.TryParseExact(
            $"{month}-01",
            "yyyy-MM-dd",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out firstDay);
}

public sealed class BulkGeneratePaymentsCommandHandler(IApplicationDbContext context)
    : IRequestHandler<BulkGeneratePaymentsCommand, BulkGenerationResult>
{
    public async Task<BulkGenerationResult> Handle(
        BulkGeneratePaymentsCommand request,
        CancellationToken cancellationToken)
    {
        BulkGeneratePaymentsCommandValidator.TryParseMonth(request.Month, out var firstDay);
        var lastDay = firstDay.AddMonths(1).AddDays(-1);

        var enrollments = await context.StudentPlans
            .Where(enrollment =>
                enrollment.DueDate >= firstDay &&
                enrollment.DueDate <= lastDay &&
                (request.PlanId == null || enrollment.PlanId == request.PlanId) &&
                (request.StudentId == null || enrollment.StudentId == request.StudentId))
            .Select(enrollment => new
            {
                enrollment.Id,
                enrollment.StudentId,
                enrollment.PlanValue,
                enrollment.DueDate
            })
            .ToListAsync(cancellationToken);

        if (enrollments.Count == 0)
        {
            return new BulkGenerationResult(request.Month, 0, 0);
        }

        var enrollmentIds = enrollments.Select(enrollment => enrollment.Id).ToList();

        var alreadyBilled = await context.Payments
            .Where(payment =>
                enrollmentIds.Contains(payment.StudentPlanId) &&
                payment.DueDate >= firstDay &&
                payment.DueDate <= lastDay)
            .Select(payment => new { payment.StudentPlanId, payment.DueDate })
            .ToListAsync(cancellationToken);

        var billed = alreadyBilled
            .Select(payment => (payment.StudentPlanId, payment.DueDate))
            .ToHashSet();

        var created = 0;

        foreach (var enrollment in enrollments)
        {
            if (!billed.Add((enrollment.Id, enrollment.DueDate)))
            {
                continue;
            }

            context.Payments.Add(new Payment
            {
                StudentId = enrollment.StudentId,
                StudentPlanId = enrollment.Id,
                PlanValue = enrollment.PlanValue,
                Value = enrollment.PlanValue,
                DueDate = enrollment.DueDate
            });

            created++;
        }

        if (created > 0)
        {
            await context.SaveChangesAsync(cancellationToken);
        }

        return new BulkGenerationResult(request.Month, created, enrollments.Count - created);
    }
}
