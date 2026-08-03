using System.Linq.Expressions;

namespace AnbuFight.Application.Payments;

/// <param name="PlanValue">Preço da matrícula no momento em que a cobrança foi emitida.</param>
/// <param name="Value">Valor efetivamente cobrado; pode diferir do preço do plano por desconto ou multa.</param>
/// <param name="DueDate">Data de vencimento da cobrança.</param>
/// <param name="PaydAt">Momento da baixa. Nulo enquanto a cobrança estiver em aberto.</param>
/// <param name="Status">Derivado de <c>PaydAt</c> e <c>DueDate</c>; nunca é armazenado.</param>
public sealed record PaymentDto(
    Guid Id,
    Guid StudentId,
    string StudentName,
    Guid StudentPlanId,
    Guid PlanId,
    string PlanName,
    decimal PlanValue,
    decimal Value,
    DateOnly DueDate,
    DateTimeOffset? PaydAt,
    PaymentStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt)
{
    /// <summary>
    /// <paramref name="today"/> is passed in (not read from the database) so the status is computed
    /// by Postgres in the same round trip, with the reference date as a query parameter.
    /// </summary>
    public static Expression<Func<Payment, PaymentDto>> Projection(DateOnly today) => payment => new PaymentDto(
        payment.Id,
        payment.StudentId,
        payment.Student.FirstName + " " + payment.Student.LastName,
        payment.StudentPlanId,
        payment.StudentPlan.PlanId,
        payment.StudentPlan.Plan.Name,
        payment.PlanValue,
        payment.Value,
        payment.DueDate,
        payment.PaydAt,
        payment.PaydAt != null
            ? PaymentStatus.Paid
            : payment.DueDate < today ? PaymentStatus.Overdue : PaymentStatus.Pending,
        payment.CreatedAt,
        payment.UpdatedAt);
}
