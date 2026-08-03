using System.Linq.Expressions;

namespace AnbuFight.Application.Plans;

/// <param name="Type">Recorrência de cobrança do plano.</param>
/// <param name="MonthsInCycle">Meses cobertos por uma cobrança do plano.</param>
/// <param name="DefaultValue">Valor de tabela, usado para sugerir o preço ao criar uma matrícula.</param>
/// <param name="EnrollmentCount">Quantidade de matrículas ativas neste plano.</param>
public sealed record PlanDto(
    Guid Id,
    string Name,
    PlanType Type,
    int MonthsInCycle,
    decimal? DefaultValue,
    int EnrollmentCount,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt)
{
    public static Expression<Func<Plan, PlanDto>> Projection { get; } = plan => new PlanDto(
        plan.Id,
        plan.Name,
        plan.Type,
        plan.Type == PlanType.Monthly ? 1
            : plan.Type == PlanType.Quarterly ? 3
            : plan.Type == PlanType.Semiannual ? 6
            : 12,
        plan.DefaultValue,
        plan.StudentPlans.Count,
        plan.CreatedAt,
        plan.UpdatedAt);
}
