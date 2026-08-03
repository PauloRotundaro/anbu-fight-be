using AnbuFight.Application.Students.Commands;
using AnbuFight.Application.UnitTests.Common;

namespace AnbuFight.Application.UnitTests.Validators;

public class CreateStudentCommandValidatorTests
{
    private readonly CreateStudentCommandValidator _validator = new(FakeGymClock.Default);

    [Fact]
    public void Accepts_a_complete_command() =>
        _validator.Validate(ValidCommand()).IsValid.ShouldBeTrue();

    [Fact]
    public void Accepts_a_command_without_a_password_the_student_simply_has_no_portal_access() =>
        _validator.Validate(ValidCommand() with { Password = null }).IsValid.ShouldBeTrue();

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Rejects_an_empty_first_name(string firstName) =>
        ShouldFailOn(ValidCommand() with { FirstName = firstName }, nameof(CreateStudentCommand.FirstName));

    // FluentValidation uses the same permissive rule as ASP.NET Core: an address just needs a single "@".
    [Theory]
    [InlineData("not-an-email")]
    [InlineData("@anbufight.com")]
    [InlineData("ryu@")]
    public void Rejects_a_malformed_email(string email) =>
        ShouldFailOn(ValidCommand() with { Email = email }, nameof(CreateStudentCommand.Email));

    [Fact]
    public void Rejects_a_birthdate_in_the_future()
    {
        var tomorrow = FakeGymClock.Default.Today.AddDays(1);

        ShouldFailOn(ValidCommand() with { Birthdate = tomorrow }, nameof(CreateStudentCommand.Birthdate));
    }

    [Fact]
    public void Rejects_a_password_shorter_than_eight_characters() =>
        ShouldFailOn(ValidCommand() with { Password = "short" }, nameof(CreateStudentCommand.Password));

    [Fact]
    public void Rejects_a_role_outside_the_enum() =>
        ShouldFailOn(ValidCommand() with { Role = (UserRole)99 }, nameof(CreateStudentCommand.Role));

    [Fact]
    public void Rejects_a_status_outside_the_enum() =>
        ShouldFailOn(ValidCommand() with { Status = (StudentStatus)99 }, nameof(CreateStudentCommand.Status));

    private void ShouldFailOn(CreateStudentCommand command, string propertyName)
    {
        var result = _validator.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(error => error.PropertyName == propertyName);
    }

    private static CreateStudentCommand ValidCommand() => new(
        FirstName: "Ryu",
        LastName: "Hayabusa",
        Birthdate: new DateOnly(1995, 6, 15),
        Email: "ryu@anbufight.com",
        PhoneNumber: "+5511999999999",
        Status: StudentStatus.Active,
        EmergencyContact: "Irene Lew",
        EmergencyPhoneNumber: "+5511988888888",
        Role: UserRole.Student,
        Password: "Anbu@Fight123");
}
