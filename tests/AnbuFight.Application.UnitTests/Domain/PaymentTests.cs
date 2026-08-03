namespace AnbuFight.Application.UnitTests.Domain;

public class PaymentTests
{
    private static readonly DateOnly Today = new(2026, 3, 10);

    [Fact]
    public void StatusOn_is_pending_while_the_due_date_has_not_passed()
    {
        var payment = PaymentDue(Today.AddDays(5));

        payment.StatusOn(Today).ShouldBe(PaymentStatus.Pending);
    }

    [Fact]
    public void StatusOn_is_pending_on_the_due_date_itself()
    {
        var payment = PaymentDue(Today);

        payment.StatusOn(Today).ShouldBe(PaymentStatus.Pending);
    }

    [Fact]
    public void StatusOn_is_overdue_after_the_due_date()
    {
        var payment = PaymentDue(Today.AddDays(-1));

        payment.StatusOn(Today).ShouldBe(PaymentStatus.Overdue);
    }

    [Fact]
    public void StatusOn_is_paid_once_settled_even_if_it_was_overdue()
    {
        var payment = PaymentDue(Today.AddDays(-30));

        payment.Settle(new DateTimeOffset(2026, 3, 10, 9, 0, 0, TimeSpan.Zero));

        payment.PaydAt.ShouldNotBeNull();
        payment.StatusOn(Today).ShouldBe(PaymentStatus.Paid);
    }

    private static Payment PaymentDue(DateOnly dueDate) => new()
    {
        StudentId = Guid.CreateVersion7(),
        StudentPlanId = Guid.CreateVersion7(),
        PlanValue = 200m,
        Value = 200m,
        DueDate = dueDate
    };
}
