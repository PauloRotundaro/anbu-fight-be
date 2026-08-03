using AnbuFight.Application.Common.Models;
using AnbuFight.Application.StudentPlans;

namespace AnbuFight.Api.IntegrationTests;

[Collection(nameof(ApiCollection))]
public class StudentPlanEndpointsTests(AnbuFightApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task Full_crud_cycle()
    {
        var client = await CreateAdminClientAsync();
        var studentId = await CreateStudentAsync(client);
        var planId = await CreatePlanAsync(client, PlanType.Monthly, $"Boxe {Guid.NewGuid():N}");

        var id = await CreateEnrollmentAsync(client, studentId, planId, 250m, new DateOnly(2026, 4, 10));

        var created = await (await client.GetAsync(new Uri($"/api/student-plans/{id}", UriKind.Relative)))
            .ReadAsync<StudentPlanDto>();

        created.StudentId.ShouldBe(studentId);
        created.PlanId.ShouldBe(planId);
        created.PlanValue.ShouldBe(250m);
        created.DueDate.ShouldBe(new DateOnly(2026, 4, 10));
        created.StudentName.ShouldBe("Ryu Hayabusa");
        created.PlanType.ShouldBe(PlanType.Monthly);

        var updated = await client.PutJsonAsync($"/api/student-plans/{id}", new
        {
            planValue = 199.90m,
            dueDate = "2026-05-05"
        });

        updated.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var afterUpdate = await (await client.GetAsync(new Uri($"/api/student-plans/{id}", UriKind.Relative)))
            .ReadAsync<StudentPlanDto>();

        afterUpdate.PlanValue.ShouldBe(199.90m);
        afterUpdate.DueDate.ShouldBe(new DateOnly(2026, 5, 5));

        var deleted = await client.DeleteAsync(new Uri($"/api/student-plans/{id}", UriKind.Relative));
        deleted.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        (await client.GetAsync(new Uri($"/api/student-plans/{id}", UriKind.Relative)))
            .StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task A_student_can_hold_several_plans_at_once()
    {
        var client = await CreateAdminClientAsync();
        var studentId = await CreateStudentAsync(client);

        await CreateEnrollmentAsync(client, studentId, await CreatePlanAsync(client));
        await CreateEnrollmentAsync(client, studentId, await CreatePlanAsync(client));

        var page = await (await client.GetAsync(
                new Uri($"/api/student-plans?studentId={studentId}", UriKind.Relative)))
            .ReadAsync<PagedResult<StudentPlanDto>>();

        page.TotalCount.ShouldBe(2);
    }

    [Fact]
    public async Task The_same_plan_cannot_be_enrolled_twice()
    {
        var client = await CreateAdminClientAsync();
        var studentId = await CreateStudentAsync(client);
        var planId = await CreatePlanAsync(client);

        await CreateEnrollmentAsync(client, studentId, planId);

        var response = await client.PostJsonAsync("/api/student-plans", new
        {
            studentId,
            planId,
            planValue = 300m,
            dueDate = "2026-06-10"
        });

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Create_reports_an_unknown_student()
    {
        var client = await CreateAdminClientAsync();

        var response = await client.PostJsonAsync("/api/student-plans", new
        {
            studentId = Guid.NewGuid(),
            planId = await CreatePlanAsync(client),
            planValue = 200m,
            dueDate = "2026-06-10"
        });

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Create_rejects_a_non_positive_price()
    {
        var client = await CreateAdminClientAsync();

        var response = await client.PostJsonAsync("/api/student-plans", new
        {
            studentId = await CreateStudentAsync(client),
            planId = await CreatePlanAsync(client),
            planValue = 0m,
            dueDate = "2026-06-10"
        });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Delete_is_blocked_while_charges_are_open()
    {
        var client = await CreateAdminClientAsync();
        var enrollmentId = await CreateEnrollmentAsync(
            client,
            await CreateStudentAsync(client),
            await CreatePlanAsync(client));

        await CreatePaymentAsync(client, enrollmentId);

        var response = await client.DeleteAsync(new Uri($"/api/student-plans/{enrollmentId}", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }
}
