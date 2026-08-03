namespace AnbuFight.Application.UnitTests.Domain;

public class PlanTypeTests
{
    [Theory]
    [InlineData(PlanType.Monthly, 1)]
    [InlineData(PlanType.Quarterly, 3)]
    [InlineData(PlanType.Semiannual, 6)]
    [InlineData(PlanType.Annual, 12)]
    public void MonthsInCycle_returns_the_recurrence_of_the_plan(PlanType type, int expectedMonths) =>
        type.MonthsInCycle().ShouldBe(expectedMonths);

    [Theory]
    [InlineData(PlanType.Monthly, "2026-01-31", "2026-02-28")]
    [InlineData(PlanType.Quarterly, "2026-01-10", "2026-04-10")]
    [InlineData(PlanType.Semiannual, "2026-01-10", "2026-07-10")]
    [InlineData(PlanType.Annual, "2026-01-10", "2027-01-10")]
    public void NextDueDate_advances_one_cycle_and_clamps_to_the_end_of_the_month(
        PlanType type,
        string currentDueDate,
        string expectedDueDate) =>
        type.NextDueDate(DateOnly.Parse(currentDueDate, CultureInfo.InvariantCulture))
            .ShouldBe(DateOnly.Parse(expectedDueDate, CultureInfo.InvariantCulture));
}
