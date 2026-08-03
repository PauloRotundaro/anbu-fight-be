using AnbuFight.Application.Common.Models;
using AnbuFight.Application.Payments;
using AnbuFight.Application.StudentPlans;
using AnbuFight.Application.Students;

namespace AnbuFight.Api.IntegrationTests;

/// <summary>
/// Covers the two layers of access control: the role policies on the endpoints and the row-level
/// rule that keeps a student inside its own data.
/// </summary>
[Collection(nameof(ApiCollection))]
public class AuthorizationTests(AnbuFightApiFactory factory) : IntegrationTestBase(factory)
{
    private const string Password = "Anbu@Fight123";

    [Fact]
    public async Task A_student_cannot_register_other_students()
    {
        var adminClient = await CreateAdminClientAsync();
        var email = UniqueEmail("student");
        await CreateStudentAsync(adminClient, email, Password);

        var studentClient = await CreateClientForAsync(email, Password);

        var response = await studentClient.PostJsonAsync("/api/students", new
        {
            firstName = "Intruder",
            lastName = "Student",
            birthdate = "1999-01-01",
            email = UniqueEmail("intruder"),
            phoneNumber = "+5511900000000",
            role = nameof(UserRole.Student)
        });

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task A_student_cannot_list_every_student()
    {
        var adminClient = await CreateAdminClientAsync();
        var email = UniqueEmail("student");
        await CreateStudentAsync(adminClient, email, Password);

        var studentClient = await CreateClientForAsync(email, Password);

        var response = await studentClient.GetAsync(new Uri("/api/students", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task A_student_reads_its_own_record_but_not_another_one()
    {
        var adminClient = await CreateAdminClientAsync();
        var email = UniqueEmail("student");
        var ownId = await CreateStudentAsync(adminClient, email, Password);
        var otherId = await CreateStudentAsync(adminClient);

        var studentClient = await CreateClientForAsync(email, Password);

        var own = await studentClient.GetAsync(new Uri($"/api/students/{ownId}", UriKind.Relative));
        own.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await own.ReadAsync<StudentDto>()).Id.ShouldBe(ownId);

        var other = await studentClient.GetAsync(new Uri($"/api/students/{otherId}", UriKind.Relative));
        other.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task A_student_listing_enrollments_only_ever_sees_its_own()
    {
        var adminClient = await CreateAdminClientAsync();
        var email = UniqueEmail("student");
        var ownId = await CreateStudentAsync(adminClient, email, Password);
        var otherId = await CreateStudentAsync(adminClient);

        var ownEnrollment = await CreateEnrollmentAsync(adminClient, ownId, await CreatePlanAsync(adminClient));
        await CreateEnrollmentAsync(adminClient, otherId, await CreatePlanAsync(adminClient));

        var studentClient = await CreateClientForAsync(email, Password);

        // No filter: the query is still narrowed to the caller.
        var mine = await (await studentClient.GetAsync(new Uri("/api/student-plans", UriKind.Relative)))
            .ReadAsync<PagedResult<StudentPlanDto>>();

        mine.TotalCount.ShouldBe(1);
        mine.Items[0].Id.ShouldBe(ownEnrollment);

        // Explicitly asking for someone else is refused.
        var someoneElse = await studentClient.GetAsync(
            new Uri($"/api/student-plans?studentId={otherId}", UriKind.Relative));

        someoneElse.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task A_student_sees_only_its_own_charges()
    {
        var adminClient = await CreateAdminClientAsync();
        var email = UniqueEmail("student");
        var ownId = await CreateStudentAsync(adminClient, email, Password);
        var otherId = await CreateStudentAsync(adminClient);

        var ownEnrollment = await CreateEnrollmentAsync(adminClient, ownId, await CreatePlanAsync(adminClient));
        var otherEnrollment = await CreateEnrollmentAsync(adminClient, otherId, await CreatePlanAsync(adminClient));

        await CreatePaymentAsync(adminClient, ownEnrollment);
        var otherPayment = await CreatePaymentAsync(adminClient, otherEnrollment);

        var studentClient = await CreateClientForAsync(email, Password);

        var mine = await (await studentClient.GetAsync(new Uri("/api/payments", UriKind.Relative)))
            .ReadAsync<PagedResult<PaymentDto>>();

        mine.TotalCount.ShouldBe(1);
        mine.Items.ShouldAllBe(payment => payment.StudentId == ownId);

        var someoneElsesCharge = await studentClient.GetAsync(
            new Uri($"/api/payments/{otherPayment}", UriKind.Relative));

        someoneElsesCharge.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task A_teacher_reads_the_roster_but_cannot_change_it()
    {
        var adminClient = await CreateAdminClientAsync();
        var email = UniqueEmail("teacher");
        await CreateTeacherAsync(adminClient, email, Password);

        var teacherClient = await CreateClientForAsync(email, Password);

        var list = await teacherClient.GetAsync(new Uri("/api/students?pageSize=1", UriKind.Relative));
        list.StatusCode.ShouldBe(HttpStatusCode.OK);

        var plans = await teacherClient.GetAsync(new Uri("/api/plans", UriKind.Relative));
        plans.StatusCode.ShouldBe(HttpStatusCode.OK);

        var write = await teacherClient.PostJsonAsync("/api/plans", new
        {
            name = $"Plano {Guid.NewGuid():N}",
            type = nameof(PlanType.Monthly)
        });

        write.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task A_tampered_token_is_rejected()
    {
        var client = CreateAnonymousClient();
        var session = await SignInAsync(client, AnbuFightApiFactory.AdminEmail, AnbuFightApiFactory.AdminPassword);

        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", session.AccessToken[..^4] + "abcd");

        var response = await client.GetAsync(new Uri("/api/students", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}
