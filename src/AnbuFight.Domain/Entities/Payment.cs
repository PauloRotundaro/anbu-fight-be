using AnbuFight.Domain.Common;
using AnbuFight.Domain.Enums;

namespace AnbuFight.Domain.Entities;

/// <summary>
/// A charge issued against an enrollment. Created as pending (<see cref="PaydAt"/> null)
/// and settled later, which is what makes overdue reporting possible.
/// </summary>
public class Payment : BaseEntity
{
    public Guid StudentId { get; set; }

    public Student Student { get; set; } = null!;

    public Guid StudentPlanId { get; set; }

    public StudentPlan StudentPlan { get; set; } = null!;

    /// <summary>Price of the enrollment at the time the charge was issued.</summary>
    public decimal PlanValue { get; set; }

    /// <summary>Amount actually charged (may differ from <see cref="PlanValue"/> due to discounts or fees).</summary>
    public decimal Value { get; set; }

    public DateOnly DueDate { get; set; }

    /// <summary>Null while the charge is open.</summary>
    public DateTimeOffset? PaydAt { get; set; }

    public PaymentStatus StatusOn(DateOnly today) =>
        PaydAt is not null ? PaymentStatus.Paid
        : DueDate < today ? PaymentStatus.Overdue
        : PaymentStatus.Pending;

    public void Settle(DateTimeOffset paidAt) => PaydAt = paidAt;
}
