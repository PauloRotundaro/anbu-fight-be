using AnbuFight.Domain.Common;
using AnbuFight.Domain.Enums;

namespace AnbuFight.Domain.Entities;

public class Plan : BaseEntity
{
    public required string Name { get; set; }

    /// <summary>Billing recurrence. The price is defined per enrollment, in <see cref="StudentPlan"/>.</summary>
    public PlanType Type { get; set; }

    /// <summary>
    /// Valor de tabela, usado apenas para sugerir o preço ao criar uma matrícula.
    /// O valor que vale é sempre o negociado em <see cref="StudentPlan.PlanValue"/>.
    /// </summary>
    public decimal? DefaultValue { get; set; }

    public ICollection<StudentPlan> StudentPlans { get; } = [];
}
