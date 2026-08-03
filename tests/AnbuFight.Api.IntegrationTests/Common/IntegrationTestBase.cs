using System.Net.Http.Headers;
using AnbuFight.Api.Infrastructure;
using AnbuFight.Application.Auth;

namespace AnbuFight.Api.IntegrationTests.Common;

/// <summary>
/// Shared plumbing: authenticated clients and the fixtures (student, plan, enrollment) most
/// scenarios need. Every record is created with a unique e-mail so tests never collide.
/// </summary>
[Collection(nameof(ApiCollection))]
public abstract class IntegrationTestBase(AnbuFightApiFactory factory)
{
    protected AnbuFightApiFactory Factory { get; } = factory;

    protected HttpClient CreateAnonymousClient() => Factory.CreateClient();

    protected Task<HttpClient> CreateAdminClientAsync() =>
        CreateClientForAsync(AnbuFightApiFactory.AdminEmail, AnbuFightApiFactory.AdminPassword);

    protected async Task<HttpClient> CreateClientForAsync(string email, string password)
    {
        var client = Factory.CreateClient();
        var session = await SignInAsync(client, email, password);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);

        return client;
    }

    protected static async Task<AuthenticationResult> SignInAsync(
        HttpClient client,
        string email,
        string password)
    {
        var response = await client.PostJsonAsync("/api/auth/login", new { email, password });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        return await response.ReadAsync<AuthenticationResult>();
    }

    protected static string UniqueEmail(string prefix) =>
        $"{prefix}.{Guid.NewGuid():N}@anbufight.com";

    protected static async Task<Guid> CreateStudentAsync(
        HttpClient client,
        string? email = null,
        string? password = null,
        StudentStatus status = StudentStatus.Active,
        DateOnly? birthdate = null)
    {
        var response = await client.PostJsonAsync("/api/students", new
        {
            firstName = "Ryu",
            lastName = "Hayabusa",
            birthdate = birthdate ?? new DateOnly(1995, 6, 15),
            email = email ?? UniqueEmail("student"),
            phoneNumber = "+5511999999999",
            status = status.ToString(),
            emergencyContact = "Irene Lew",
            emergencyPhoneNumber = "+5511988888888",
            role = nameof(UserRole.Student),
            password
        });

        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        return (await response.ReadAsync<CreatedResponse>()).Id;
    }

    protected static async Task<Guid> CreateTeacherAsync(
        HttpClient client,
        string? email = null,
        string? password = null)
    {
        var response = await client.PostJsonAsync("/api/teachers", new
        {
            firstName = "Hayate",
            lastName = "Mochizuki",
            birthdate = "1988-02-20",
            email = email ?? UniqueEmail("teacher"),
            phoneNumber = "+5511977777777",
            role = nameof(UserRole.Teacher),
            password
        });

        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        return (await response.ReadAsync<CreatedResponse>()).Id;
    }

    protected static async Task<Guid> CreatePlanAsync(
        HttpClient client,
        PlanType type = PlanType.Monthly,
        string? name = null)
    {
        var response = await client.PostJsonAsync("/api/plans", new
        {
            name = name ?? $"Plano {Guid.NewGuid():N}",
            type = type.ToString()
        });

        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        return (await response.ReadAsync<CreatedResponse>()).Id;
    }

    protected static async Task<Guid> CreateEnrollmentAsync(
        HttpClient client,
        Guid studentId,
        Guid planId,
        decimal planValue = 200m,
        DateOnly? dueDate = null)
    {
        var response = await client.PostJsonAsync("/api/student-plans", new
        {
            studentId,
            planId,
            planValue,
            dueDate = dueDate ?? new DateOnly(2026, 4, 10)
        });

        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        return (await response.ReadAsync<CreatedResponse>()).Id;
    }

    protected static async Task<Guid> CreatePaymentAsync(
        HttpClient client,
        Guid studentPlanId,
        decimal? value = null,
        DateOnly? dueDate = null)
    {
        var response = await client.PostJsonAsync("/api/payments", new
        {
            studentPlanId,
            value,
            dueDate
        });

        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        return (await response.ReadAsync<CreatedResponse>()).Id;
    }

    protected static async Task<Guid> CreateClassAsync(
        HttpClient client,
        Guid teacherId,
        IReadOnlyList<int>? daysOfWeek = null,
        string startTime = "19:00",
        string endTime = "20:00",
        int? capacity = null,
        Modality modality = Modality.MuayThai)
    {
        var response = await client.PostJsonAsync("/api/classes", new
        {
            name = $"Turma {Guid.NewGuid():N}",
            modality = modality.ToString(),
            daysOfWeek = daysOfWeek ?? [1, 2, 3, 4, 5, 6, 0],
            startTime,
            endTime,
            teacherId,
            capacity,
            isActive = true
        });

        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        return (await response.ReadAsync<CreatedResponse>()).Id;
    }
}
