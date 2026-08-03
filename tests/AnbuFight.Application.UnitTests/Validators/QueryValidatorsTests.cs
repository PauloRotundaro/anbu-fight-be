using AnbuFight.Application.Common.Models;
using AnbuFight.Application.Payments.Queries;
using AnbuFight.Application.StudentPlans.Commands;
using AnbuFight.Application.Students.Queries;

namespace AnbuFight.Application.UnitTests.Validators;

public class QueryValidatorsTests
{
    [Fact]
    public void GetStudents_rejects_a_page_size_above_the_hard_ceiling()
    {
        var validator = new GetStudentsQueryValidator();

        var result = validator.Validate(new GetStudentsQuery(PageSize: PagingDefaults.MaxPageSize + 1));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(error => error.PropertyName == nameof(GetStudentsQuery.PageSize));
    }

    [Fact]
    public void GetStudents_accepts_the_defaults() =>
        new GetStudentsQueryValidator().Validate(new GetStudentsQuery()).IsValid.ShouldBeTrue();

    [Fact]
    public void GetPayments_rejects_an_inverted_due_date_range()
    {
        var validator = new GetPaymentsQueryValidator();

        var result = validator.Validate(new GetPaymentsQuery(
            DueFrom: new DateOnly(2026, 5, 1),
            DueTo: new DateOnly(2026, 4, 1)));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(error => error.PropertyName == nameof(GetPaymentsQuery.DueTo));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-50)]
    public void CreateStudentPlan_rejects_a_non_positive_price(decimal planValue)
    {
        var validator = new CreateStudentPlanCommandValidator();

        var result = validator.Validate(new CreateStudentPlanCommand(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            planValue,
            new DateOnly(2026, 4, 10)));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(error => error.PropertyName == nameof(CreateStudentPlanCommand.PlanValue));
    }
}
