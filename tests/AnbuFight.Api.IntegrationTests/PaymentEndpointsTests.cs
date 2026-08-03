using AnbuFight.Application.Common.Models;
using AnbuFight.Application.Payments;
using AnbuFight.Application.StudentPlans;

namespace AnbuFight.Api.IntegrationTests;

[Collection(nameof(ApiCollection))]
public class PaymentEndpointsTests(AnbuFightApiFactory factory) : IntegrationTestBase(factory)
{
    private static readonly DateOnly FutureDueDate = new(2099, 4, 10);
    private static readonly DateOnly PastDueDate = new(2020, 1, 10);

    [Fact]
    public async Task Full_crud_cycle()
    {
        var client = await CreateAdminClientAsync();
        var studentId = await CreateStudentAsync(client);
        var enrollmentId = await CreateEnrollmentAsync(
            client, studentId, await CreatePlanAsync(client), 250m, FutureDueDate);

        var id = await CreatePaymentAsync(client, enrollmentId);

        // Value and due date default to the enrollment's.
        var created = await GetPaymentAsync(client, id);
        created.StudentId.ShouldBe(studentId);
        created.StudentPlanId.ShouldBe(enrollmentId);
        created.PlanValue.ShouldBe(250m);
        created.Value.ShouldBe(250m);
        created.DueDate.ShouldBe(FutureDueDate);
        created.PaydAt.ShouldBeNull();
        created.Status.ShouldBe(PaymentStatus.Pending);

        var updated = await client.PutJsonAsync($"/api/payments/{id}", new
        {
            value = 180m,
            dueDate = "2099-04-15"
        });

        updated.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var afterUpdate = await GetPaymentAsync(client, id);
        afterUpdate.Value.ShouldBe(180m);
        afterUpdate.DueDate.ShouldBe(new DateOnly(2099, 4, 15));

        var deleted = await client.DeleteAsync(new Uri($"/api/payments/{id}", UriKind.Relative));
        deleted.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        (await client.GetAsync(new Uri($"/api/payments/{id}", UriKind.Relative)))
            .StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Settling_marks_the_charge_as_paid_and_rolls_the_enrollment_forward()
    {
        var client = await CreateAdminClientAsync();
        var enrollmentId = await CreateEnrollmentAsync(
            client,
            await CreateStudentAsync(client),
            await CreatePlanAsync(client, PlanType.Monthly),
            200m,
            FutureDueDate);

        var paymentId = await CreatePaymentAsync(client, enrollmentId);

        var settled = await client.PatchJsonAsync($"/api/payments/{paymentId}/settle", new { paydAt = (string?)null });
        settled.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var payment = await GetPaymentAsync(client, paymentId);
        payment.Status.ShouldBe(PaymentStatus.Paid);
        payment.PaydAt.ShouldNotBeNull();

        var enrollment = await (await client.GetAsync(
                new Uri($"/api/student-plans/{enrollmentId}", UriKind.Relative)))
            .ReadAsync<StudentPlanDto>();

        enrollment.DueDate.ShouldBe(FutureDueDate.AddMonths(1));
    }

    [Fact]
    public async Task A_settled_charge_can_neither_be_settled_again_nor_edited()
    {
        var client = await CreateAdminClientAsync();
        var enrollmentId = await CreateEnrollmentAsync(
            client, await CreateStudentAsync(client), await CreatePlanAsync(client), 200m, FutureDueDate);

        var paymentId = await CreatePaymentAsync(client, enrollmentId);
        await client.PatchJsonAsync($"/api/payments/{paymentId}/settle", new { paydAt = (string?)null });

        var settleAgain = await client.PatchJsonAsync(
            $"/api/payments/{paymentId}/settle", new { paydAt = (string?)null });
        settleAgain.StatusCode.ShouldBe(HttpStatusCode.Conflict);

        var edit = await client.PutJsonAsync($"/api/payments/{paymentId}", new
        {
            value = 10m,
            dueDate = "2099-05-10"
        });
        edit.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task An_unpaid_charge_past_its_due_date_is_reported_as_overdue()
    {
        var client = await CreateAdminClientAsync();
        var studentId = await CreateStudentAsync(client);
        var enrollmentId = await CreateEnrollmentAsync(
            client, studentId, await CreatePlanAsync(client), 200m, PastDueDate);

        var paymentId = await CreatePaymentAsync(client, enrollmentId);

        (await GetPaymentAsync(client, paymentId)).Status.ShouldBe(PaymentStatus.Overdue);

        var overdue = await (await client.GetAsync(new Uri(
                $"/api/payments?studentId={studentId}&status={nameof(PaymentStatus.Overdue)}", UriKind.Relative)))
            .ReadAsync<PagedResult<PaymentDto>>();

        overdue.TotalCount.ShouldBe(1);
        overdue.Items[0].Id.ShouldBe(paymentId);

        var pending = await (await client.GetAsync(new Uri(
                $"/api/payments?studentId={studentId}&status={nameof(PaymentStatus.Pending)}", UriKind.Relative)))
            .ReadAsync<PagedResult<PaymentDto>>();

        pending.TotalCount.ShouldBe(0);
    }

    [Fact]
    public async Task A_charge_can_be_recorded_as_already_paid()
    {
        var client = await CreateAdminClientAsync();
        var enrollmentId = await CreateEnrollmentAsync(
            client, await CreateStudentAsync(client), await CreatePlanAsync(client), 200m, FutureDueDate);

        var response = await client.PostJsonAsync("/api/payments", new
        {
            studentPlanId = enrollmentId,
            value = 200m,
            dueDate = FutureDueDate,
            paydAt = DateTimeOffset.UtcNow
        });

        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        var payment = await GetPaymentAsync(
            client, (await response.ReadAsync<Api.Infrastructure.CreatedResponse>()).Id);

        payment.Status.ShouldBe(PaymentStatus.Paid);
    }

    [Fact]
    public async Task Charges_can_be_filtered_by_due_date_range()
    {
        var client = await CreateAdminClientAsync();
        var studentId = await CreateStudentAsync(client);
        var enrollmentId = await CreateEnrollmentAsync(
            client, studentId, await CreatePlanAsync(client), 200m, FutureDueDate);

        await CreatePaymentAsync(client, enrollmentId, dueDate: new DateOnly(2099, 1, 10));
        await CreatePaymentAsync(client, enrollmentId, dueDate: new DateOnly(2099, 6, 10));

        var firstQuarter = await (await client.GetAsync(new Uri(
                $"/api/payments?studentId={studentId}&dueFrom=2099-01-01&dueTo=2099-03-31", UriKind.Relative)))
            .ReadAsync<PagedResult<PaymentDto>>();

        firstQuarter.TotalCount.ShouldBe(1);
        firstQuarter.Items[0].DueDate.ShouldBe(new DateOnly(2099, 1, 10));
    }

    [Fact]
    public async Task Create_reports_an_unknown_enrollment()
    {
        var client = await CreateAdminClientAsync();

        var response = await client.PostJsonAsync("/api/payments", new
        {
            studentPlanId = Guid.NewGuid(),
            value = 200m
        });

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Settle_reports_an_unknown_charge()
    {
        var client = await CreateAdminClientAsync();

        var response = await client.PatchJsonAsync(
            $"/api/payments/{Guid.NewGuid()}/settle", new { paydAt = (string?)null });

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    private static async Task<PaymentDto> GetPaymentAsync(HttpClient client, Guid id) =>
        await (await client.GetAsync(new Uri($"/api/payments/{id}", UriKind.Relative))).ReadAsync<PaymentDto>();
}
