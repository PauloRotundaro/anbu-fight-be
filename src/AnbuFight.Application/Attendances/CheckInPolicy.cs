using Microsoft.Extensions.Options;

namespace AnbuFight.Application.Attendances;

/// <summary>
/// Decide se um aluno pode treinar. A mesma avaliação alimenta a tela (para mostrar o aviso antes
/// do clique) e o próprio check-in — a regra é imposta no servidor, não sugerida ao cliente.
/// </summary>
public sealed class CheckInPolicy(
    IApplicationDbContext context,
    IGymClock clock,
    IOptions<GymOptions> options)
{
    private readonly GymOptions _options = options.Value;

    public int GraceDays => _options.OverdueGraceDays;

    public int CheckInWindowMinutesBefore => _options.CheckInWindowMinutesBefore;

    public async Task<CheckInEligibilityDto> EvaluateAsync(Guid studentId, CancellationToken cancellationToken)
    {
        var today = clock.Today;

        var student = await context.Students
            .AsNoTracking()
            .Where(entity => entity.Id == studentId)
            .Select(entity => new { entity.Status })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(nameof(Student), studentId);

        var overdue = await context.Payments
            .AsNoTracking()
            .Where(payment =>
                payment.StudentId == studentId &&
                payment.PaydAt == null &&
                payment.DueDate < today)
            .GroupBy(_ => 1)
            .Select(group => new
            {
                Count = group.Count(),
                Amount = group.Sum(payment => payment.Value),
                OldestDueDate = group.Min(payment => payment.DueDate)
            })
            .FirstOrDefaultAsync(cancellationToken);

        var daysOverdue = overdue is null ? 0 : today.DayNumber - overdue.OldestDueDate.DayNumber;
        var hasOverdueDebt = overdue is not null;
        var graceDaysRemaining = Math.Max(0, _options.OverdueGraceDays - daysOverdue);

        var hasEnrollment = await context.StudentPlans
            .AnyAsync(enrollment => enrollment.StudentId == studentId, cancellationToken);

        // A ordem importa: o primeiro impedimento encontrado é o que a tela explica ao aluno.
        var reason =
            student.Status != StudentStatus.Active ? CheckInBlockReason.InactiveStudent
            : !hasEnrollment ? CheckInBlockReason.NoActiveEnrollment
            : daysOverdue > _options.OverdueGraceDays ? CheckInBlockReason.OverdueLimitExceeded
            : CheckInBlockReason.Ok;

        return new CheckInEligibilityDto(
            studentId,
            reason == CheckInBlockReason.Ok,
            reason,
            hasOverdueDebt,
            daysOverdue,
            overdue?.Amount ?? 0m,
            overdue?.Count ?? 0,
            _options.OverdueGraceDays,
            graceDaysRemaining);
    }

    /// <summary>Mensagem exibida ao aluno quando o check-in é recusado.</summary>
    public static string Explain(CheckInEligibilityDto eligibility) => eligibility.Reason switch
    {
        CheckInBlockReason.InactiveStudent =>
            "Seu cadastro não está ativo. Procure a recepção da academia.",
        CheckInBlockReason.NoActiveEnrollment =>
            "Você ainda não tem um plano ativo. Procure a recepção da academia.",
        CheckInBlockReason.OverdueLimitExceeded =>
            $"Você tem {eligibility.OverdueCount} cobrança(s) em atraso há {eligibility.DaysOverdue} dias. " +
            $"O check-in é liberado com até {eligibility.GraceDays} dias de atraso.",
        _ => "Check-in liberado."
    };
}
