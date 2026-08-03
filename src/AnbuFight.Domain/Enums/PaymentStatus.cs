namespace AnbuFight.Domain.Enums;

/// <summary>
/// Derived from <c>PaydAt</c> and <c>DueDate</c> — never stored, so it can never drift.
/// </summary>
public enum PaymentStatus
{
    /// <summary>Not paid yet and still within the due date.</summary>
    Pending = 1,

    /// <summary>Settled.</summary>
    Paid = 2,

    /// <summary>Not paid and past the due date.</summary>
    Overdue = 3
}
