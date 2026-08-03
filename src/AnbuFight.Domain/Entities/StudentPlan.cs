using AnbuFight.Domain.Common;
using AnbuFight.Domain.Enums;

namespace AnbuFight.Domain.Entities;

/// <summary>
/// Enrollment of a student in a plan (N-M). The price is stored here because it is
/// negotiated per student and must not change retroactively when the plan changes.
/// </summary>
public class StudentPlan : BaseEntity
{
    public Guid StudentId { get; set; }

    public Student Student { get; set; } = null!;

    public Guid PlanId { get; set; }

    public Plan Plan { get; set; } = null!;

    public decimal PlanValue { get; set; }

    /// <summary>Due date of the next charge for this enrollment.</summary>
    public DateOnly DueDate { get; set; }

    public ICollection<Payment> Payments { get; } = [];

    /// <summary>
    /// Rolls the enrollment to the next billing cycle once the charge for the current cycle is settled.
    /// Charges for past or future cycles leave the schedule untouched.
    /// </summary>
    public void AdvanceDueDateFor(Payment settledPayment, PlanType planType)
    {
        if (settledPayment.DueDate == DueDate)
        {
            DueDate = planType.NextDueDate(DueDate);
        }
    }
}
