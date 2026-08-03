using AnbuFight.Application.Common.Models;
using AnbuFight.Application.Plans;

namespace AnbuFight.Api.IntegrationTests;

[Collection(nameof(ApiCollection))]
public class PlanEndpointsTests(AnbuFightApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task Full_crud_cycle()
    {
        var client = await CreateAdminClientAsync();
        var name = $"Boxe Mensal {Guid.NewGuid():N}";

        var id = await CreatePlanAsync(client, PlanType.Monthly, name);

        var created = await (await client.GetAsync(new Uri($"/api/plans/{id}", UriKind.Relative)))
            .ReadAsync<PlanDto>();

        created.Name.ShouldBe(name);
        created.Type.ShouldBe(PlanType.Monthly);
        created.MonthsInCycle.ShouldBe(1);
        created.EnrollmentCount.ShouldBe(0);

        var updated = await client.PutJsonAsync($"/api/plans/{id}", new
        {
            name,
            type = nameof(PlanType.Quarterly)
        });

        updated.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var afterUpdate = await (await client.GetAsync(new Uri($"/api/plans/{id}", UriKind.Relative)))
            .ReadAsync<PlanDto>();

        afterUpdate.Type.ShouldBe(PlanType.Quarterly);
        afterUpdate.MonthsInCycle.ShouldBe(3);

        var deleted = await client.DeleteAsync(new Uri($"/api/plans/{id}", UriKind.Relative));
        deleted.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        (await client.GetAsync(new Uri($"/api/plans/{id}", UriKind.Relative)))
            .StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Create_rejects_a_duplicated_name()
    {
        var client = await CreateAdminClientAsync();
        var name = $"Muay Thai {Guid.NewGuid():N}";

        await CreatePlanAsync(client, PlanType.Monthly, name);

        var response = await client.PostJsonAsync("/api/plans", new
        {
            name = name.ToUpperInvariant(),
            type = nameof(PlanType.Annual)
        });

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Create_rejects_an_unknown_recurrence()
    {
        var client = await CreateAdminClientAsync();

        var response = await client.PostJsonAsync("/api/plans", new
        {
            name = $"Plano {Guid.NewGuid():N}",
            type = "Weekly"
        });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Delete_is_blocked_while_students_are_enrolled()
    {
        var client = await CreateAdminClientAsync();
        var planId = await CreatePlanAsync(client);
        var studentId = await CreateStudentAsync(client);

        await CreateEnrollmentAsync(client, studentId, planId);

        var response = await client.DeleteAsync(new Uri($"/api/plans/{planId}", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task List_filters_by_recurrence()
    {
        var client = await CreateAdminClientAsync();
        var marker = $"Anual{Guid.NewGuid():N}";

        await CreatePlanAsync(client, PlanType.Annual, marker);

        var page = await (await client.GetAsync(
                new Uri($"/api/plans?search={marker}&type={nameof(PlanType.Annual)}", UriKind.Relative)))
            .ReadAsync<PagedResult<PlanDto>>();

        page.TotalCount.ShouldBe(1);
        page.Items[0].Type.ShouldBe(PlanType.Annual);

        var otherRecurrence = await (await client.GetAsync(
                new Uri($"/api/plans?search={marker}&type={nameof(PlanType.Monthly)}", UriKind.Relative)))
            .ReadAsync<PagedResult<PlanDto>>();

        otherRecurrence.TotalCount.ShouldBe(0);
    }

    [Fact]
    public async Task EnrollmentCount_reflects_the_active_enrollments()
    {
        var client = await CreateAdminClientAsync();
        var planId = await CreatePlanAsync(client);

        await CreateEnrollmentAsync(client, await CreateStudentAsync(client), planId);
        await CreateEnrollmentAsync(client, await CreateStudentAsync(client), planId);

        var plan = await (await client.GetAsync(new Uri($"/api/plans/{planId}", UriKind.Relative)))
            .ReadAsync<PlanDto>();

        plan.EnrollmentCount.ShouldBe(2);
    }
}
