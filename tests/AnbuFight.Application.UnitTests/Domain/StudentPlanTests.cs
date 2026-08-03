namespace AnbuFight.Application.UnitTests.Domain;

public class StudentPlanTests
{
    [Fact]
    public void AdvanceDueDateFor_rolls_to_the_next_cycle_when_the_current_charge_is_settled()
    {
        var enrollment = EnrollmentDue(new DateOnly(2026, 3, 10));
        var payment = PaymentDue(new DateOnly(2026, 3, 10));

        enrollment.AdvanceDueDateFor(payment, PlanType.Monthly);

        enrollment.DueDate.ShouldBe(new DateOnly(2026, 4, 10));
    }

    [Fact]
    public void AdvanceDueDateFor_leaves_the_schedule_untouched_for_a_charge_of_another_cycle()
    {
        var enrollment = EnrollmentDue(new DateOnly(2026, 3, 10));
        var overdueChargeFromLastMonth = PaymentDue(new DateOnly(2026, 2, 10));

        enrollment.AdvanceDueDateFor(overdueChargeFromLastMonth, PlanType.Monthly);

        enrollment.DueDate.ShouldBe(new DateOnly(2026, 3, 10));
    }

    private static StudentPlan EnrollmentDue(DateOnly dueDate) => new()
    {
        StudentId = Guid.CreateVersion7(),
        PlanId = Guid.CreateVersion7(),
        PlanValue = 200m,
        DueDate = dueDate
    };

    private static Payment PaymentDue(DateOnly dueDate) => new()
    {
        StudentId = Guid.CreateVersion7(),
        StudentPlanId = Guid.CreateVersion7(),
        PlanValue = 200m,
        Value = 200m,
        DueDate = dueDate
    };
}
