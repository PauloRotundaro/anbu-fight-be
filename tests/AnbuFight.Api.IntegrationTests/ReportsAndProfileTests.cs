using AnbuFight.Application.Payments.Commands;
using AnbuFight.Application.Plans;
using AnbuFight.Application.Reports;
using AnbuFight.Application.Students;
using AnbuFight.Application.Students.Queries;

namespace AnbuFight.Api.IntegrationTests;

/// <summary>
/// Consultas que existem porque o cliente não conseguiria montá-las sem varrer a base:
/// aniversariantes, resumo financeiro e resumo do aluno. Mais o perfil do próprio aluno.
/// </summary>
[Collection(nameof(ApiCollection))]
public class ReportsAndProfileTests(AnbuFightApiFactory factory) : IntegrationTestBase(factory)
{
    private const string Password = "Anbu@Fight123";

    private static DateOnly GymToday => DateOnly.FromDateTime(DateTime.UtcNow.AddHours(-3));

    [Fact]
    public async Task Birthdays_within_the_window_come_ordered_by_proximity()
    {
        var admin = await CreateAdminClientAsync();
        var today = GymToday;

        var inThreeDays = today.AddDays(3);
        var inTenDays = today.AddDays(10);

        var soonId = await CreateStudentAsync(
            admin, birthdate: new DateOnly(1990, inThreeDays.Month, inThreeDays.Day));
        var laterId = await CreateStudentAsync(
            admin, birthdate: new DateOnly(1990, inTenDays.Month, inTenDays.Day));

        var birthdays = await (await admin.GetAsync(
                new Uri("/api/students/birthdays?daysAhead=15", UriKind.Relative)))
            .ReadAsync<IReadOnlyList<BirthdayStudentDto>>();

        var soon = birthdays.Single(student => student.Id == soonId);
        var later = birthdays.Single(student => student.Id == laterId);

        soon.DaysUntilBirthday.ShouldBe(3);
        later.DaysUntilBirthday.ShouldBe(10);
        soon.Age.ShouldBe(inThreeDays.Year - 1990);
        birthdays.Select(student => student.DaysUntilBirthday).ShouldBeInOrder();
    }

    [Fact]
    public async Task Birthdays_outside_the_window_are_left_out()
    {
        var admin = await CreateAdminClientAsync();
        var farAway = GymToday.AddDays(60);

        var id = await CreateStudentAsync(admin, birthdate: new DateOnly(1990, farAway.Month, farAway.Day));

        var birthdays = await (await admin.GetAsync(
                new Uri("/api/students/birthdays?daysAhead=7", UriKind.Relative)))
            .ReadAsync<IReadOnlyList<BirthdayStudentDto>>();

        birthdays.ShouldNotContain(student => student.Id == id);
    }

    [Fact]
    public async Task A_student_updates_the_own_contact_details()
    {
        var admin = await CreateAdminClientAsync();
        var email = UniqueEmail("perfil");
        var id = await CreateStudentAsync(admin, email, Password);

        var student = await CreateClientForAsync(email, Password);

        var updated = await student.PutJsonAsync("/api/students/me", new
        {
            firstName = "Ryu",
            lastName = "Hayabusa",
            phoneNumber = "+5511911112222",
            emergencyContact = "Ayane",
            emergencyPhoneNumber = "+5511933334444"
        });

        updated.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var profile = await (await student.GetAsync(new Uri($"/api/students/{id}", UriKind.Relative)))
            .ReadAsync<StudentDto>();

        profile.PhoneNumber.ShouldBe("+5511911112222");
        profile.EmergencyContact.ShouldBe("Ayane");

        // O e-mail continua sendo a identidade de acesso: não muda por aqui.
        profile.Email.ShouldBe(email);
    }

    [Fact]
    public async Task The_student_summary_avoids_the_cascade_of_requests()
    {
        var admin = await CreateAdminClientAsync();
        var studentId = await CreateStudentAsync(admin);
        var enrollmentId = await CreateEnrollmentAsync(
            admin, studentId, await CreatePlanAsync(admin), 300m, GymToday.AddDays(10));

        await CreatePaymentAsync(admin, enrollmentId);

        var summary = await (await admin.GetAsync(
                new Uri($"/api/students/{studentId}/summary", UriKind.Relative)))
            .ReadAsync<StudentSummaryDto>();

        summary.Student.Id.ShouldBe(studentId);
        summary.Enrollments.Count.ShouldBe(1);
        summary.Enrollments[0].PlanValue.ShouldBe(300m);
        summary.RecentPayments.Count.ShouldBe(1);
        summary.RecentAttendances.ShouldBeEmpty();
        summary.Eligibility.CanCheckIn.ShouldBeTrue();
    }

    [Fact]
    public async Task The_financial_summary_aggregates_the_period()
    {
        var admin = await CreateAdminClientAsync();
        var studentId = await CreateStudentAsync(admin);
        var enrollmentId = await CreateEnrollmentAsync(
            admin, studentId, await CreatePlanAsync(admin), 250m, new DateOnly(2031, 5, 10));

        var paidId = await CreatePaymentAsync(admin, enrollmentId, 250m, new DateOnly(2031, 5, 10));
        await CreatePaymentAsync(admin, enrollmentId, 150m, new DateOnly(2031, 5, 20));

        await admin.PatchJsonAsync($"/api/payments/{paidId}/settle", new { paydAt = (string?)null });

        var summary = await (await admin.GetAsync(new Uri(
                "/api/reports/financial-summary?from=2031-05-01&to=2031-05-31", UriKind.Relative)))
            .ReadAsync<FinancialSummaryDto>();

        summary.BilledTotal.ShouldBe(400m);
        summary.PendingTotal.ShouldBe(150m);
        summary.OverdueTotal.ShouldBe(0m);
        summary.ActiveStudents.ShouldBeGreaterThan(0);
        summary.RevenueByMonth.ShouldContain(month => month.Month == "2031-05" && month.Billed == 400m);
    }

    [Fact]
    public async Task The_financial_summary_counts_what_is_overdue()
    {
        var admin = await CreateAdminClientAsync();
        var studentId = await CreateStudentAsync(admin);
        var pastDue = new DateOnly(2020, 3, 10);
        var enrollmentId = await CreateEnrollmentAsync(
            admin, studentId, await CreatePlanAsync(admin), 180m, pastDue);

        await CreatePaymentAsync(admin, enrollmentId, 180m, pastDue);

        var summary = await (await admin.GetAsync(new Uri(
                "/api/reports/financial-summary?from=2020-03-01&to=2020-03-31", UriKind.Relative)))
            .ReadAsync<FinancialSummaryDto>();

        summary.OverdueTotal.ShouldBe(180m);
        summary.OverdueCount.ShouldBe(1);
        summary.DefaultRate.ShouldBe(1m);
    }

    [Fact]
    public async Task A_student_cannot_read_the_financial_summary()
    {
        var admin = await CreateAdminClientAsync();
        var email = UniqueEmail("aluno");
        await CreateStudentAsync(admin, email, Password);

        var student = await CreateClientForAsync(email, Password);

        var response = await student.GetAsync(new Uri("/api/reports/financial-summary", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Bulk_generation_issues_the_charges_of_the_month_once()
    {
        var admin = await CreateAdminClientAsync();
        var planId = await CreatePlanAsync(admin);

        await CreateEnrollmentAsync(
            admin, await CreateStudentAsync(admin), planId, 200m, new DateOnly(2032, 7, 5));
        await CreateEnrollmentAsync(
            admin, await CreateStudentAsync(admin), planId, 250m, new DateOnly(2032, 7, 15));

        var first = await (await admin.PostJsonAsync(
                "/api/payments/bulk-generate", new { month = "2032-07", planId }))
            .ReadAsync<BulkGenerationResult>();

        first.Created.ShouldBe(2);
        first.Skipped.ShouldBe(0);

        // Idempotente: rodar de novo não duplica cobrança.
        var second = await (await admin.PostJsonAsync(
                "/api/payments/bulk-generate", new { month = "2032-07", planId }))
            .ReadAsync<BulkGenerationResult>();

        second.Created.ShouldBe(0);
        second.Skipped.ShouldBe(2);
    }

    [Fact]
    public async Task Bulk_generation_validates_the_month()
    {
        var admin = await CreateAdminClientAsync();

        var response = await admin.PostJsonAsync("/api/payments/bulk-generate", new { month = "julho/2032" });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task A_plan_carries_a_suggested_price()
    {
        var admin = await CreateAdminClientAsync();

        var created = await admin.PostJsonAsync("/api/plans", new
        {
            name = $"Plano {Guid.NewGuid():N}",
            type = nameof(PlanType.Monthly),
            defaultValue = 199.90m
        });

        created.StatusCode.ShouldBe(HttpStatusCode.Created);

        var id = (await created.ReadAsync<Api.Infrastructure.CreatedResponse>()).Id;

        var plan = await (await admin.GetAsync(new Uri($"/api/plans/{id}", UriKind.Relative)))
            .ReadAsync<PlanDto>();

        plan.DefaultValue.ShouldBe(199.90m);
    }
}
