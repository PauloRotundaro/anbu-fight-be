namespace AnbuFight.Domain.Enums;

/// <summary>Billing recurrence of a plan; drives the next due date of an enrollment.</summary>
public enum PlanType
{
    Monthly = 1,
    Quarterly = 2,
    Semiannual = 3,
    Annual = 4
}

public static class PlanTypeExtensions
{
    /// <summary>Number of months covered by a single charge of the plan.</summary>
    public static int MonthsInCycle(this PlanType type) => type switch
    {
        PlanType.Monthly => 1,
        PlanType.Quarterly => 3,
        PlanType.Semiannual => 6,
        PlanType.Annual => 12,
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unsupported plan type.")
    };

    /// <summary>Due date of the charge that follows <paramref name="currentDueDate"/>.</summary>
    public static DateOnly NextDueDate(this PlanType type, DateOnly currentDueDate) =>
        currentDueDate.AddMonths(type.MonthsInCycle());
}
