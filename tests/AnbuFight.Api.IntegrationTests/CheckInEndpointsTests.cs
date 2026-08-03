using AnbuFight.Api.Infrastructure;
using AnbuFight.Application.Attendances;
using AnbuFight.Application.Attendances.Queries;
using AnbuFight.Application.ClassSessions;
using AnbuFight.Application.Common.Models;

namespace AnbuFight.Api.IntegrationTests;

/// <summary>
/// Fluxo central do portal: a aula de hoje, o botão de check-in e as regras que o servidor impõe.
/// </summary>
[Collection(nameof(ApiCollection))]
public class CheckInEndpointsTests(AnbuFightApiFactory factory) : IntegrationTestBase(factory)
{
    private const string Password = "Anbu@Fight123";

    /// <summary>Data corrente no fuso da academia (America/Sao_Paulo).</summary>
    private static DateOnly GymToday => DateOnly.FromDateTime(DateTime.UtcNow.AddHours(-3));

    [Fact]
    public async Task Todays_sessions_are_materialized_and_the_student_can_check_in()
    {
        var (admin, student, _) = await SetupAsync();
        var classId = await CreateAllDayClassAsync(admin);

        var sessions = await GetTodayAsync(student);
        var session = sessions.Single(entry => entry.ClassId == classId);

        session.Date.ShouldBe(GymToday);
        session.AlreadyCheckedIn.ShouldBeFalse();
        session.CanCheckIn.ShouldBeTrue();
        session.CheckInUnavailableReason.ShouldBeNull();

        var checkedIn = await student.PostJsonAsync(
            "/api/attendances/check-in", new { classSessionId = session.Id });

        checkedIn.StatusCode.ShouldBe(HttpStatusCode.Created);

        var afterCheckIn = (await GetTodayAsync(student)).Single(entry => entry.ClassId == classId);
        afterCheckIn.AlreadyCheckedIn.ShouldBeTrue();
        afterCheckIn.AttendanceCount.ShouldBe(1);
        afterCheckIn.CanCheckIn.ShouldBeFalse();
        afterCheckIn.CheckInUnavailableReason.ShouldNotBeNull();
    }

    [Fact]
    public async Task Checking_in_twice_is_refused()
    {
        var (admin, student, _) = await SetupAsync();
        var sessionId = await TodaySessionAsync(admin, student);

        await student.PostJsonAsync("/api/attendances/check-in", new { classSessionId = sessionId });

        var again = await student.PostJsonAsync("/api/attendances/check-in", new { classSessionId = sessionId });

        again.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task A_cancelled_session_accepts_no_check_in()
    {
        var (admin, student, _) = await SetupAsync();
        var classId = await CreateAllDayClassAsync(admin);
        var sessionId = (await GetTodayAsync(admin)).Single(entry => entry.ClassId == classId).Id;

        var cancelled = await admin.PatchJsonAsync(
            $"/api/class-sessions/{sessionId}/cancel", new { reason = "Feriado" });
        cancelled.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var response = await student.PostJsonAsync(
            "/api/attendances/check-in", new { classSessionId = sessionId });
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);

        var session = (await GetTodayAsync(student)).Single(entry => entry.ClassId == classId);
        session.IsCancelled.ShouldBeTrue();
        session.CancellationReason.ShouldBe("Feriado");
        session.CanCheckIn.ShouldBeFalse();

        // Cancelar de novo não faz sentido.
        var again = await admin.PatchJsonAsync(
            $"/api/class-sessions/{sessionId}/cancel", new { reason = "De novo" });
        again.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task A_full_class_accepts_no_check_in()
    {
        var (admin, student, _) = await SetupAsync();
        var classId = await CreateAllDayClassAsync(admin, capacity: 1);
        var sessionId = (await GetTodayAsync(admin)).Single(entry => entry.ClassId == classId).Id;

        // A única vaga é ocupada por outro aluno, via chamada manual.
        var otherStudent = await CreateStudentAsync(admin);
        var filled = await admin.PostJsonAsync("/api/attendances", new
        {
            studentId = otherStudent,
            classSessionId = sessionId
        });
        filled.StatusCode.ShouldBe(HttpStatusCode.Created);

        var response = await student.PostJsonAsync(
            "/api/attendances/check-in", new { classSessionId = sessionId });

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Check_in_only_opens_close_to_the_class()
    {
        var (admin, student, _) = await SetupAsync();
        var classId = await CreateAllDayClassAsync(admin);

        // Uma ocorrência daqui a três dias está muito longe da janela de 60 minutos.
        var future = GymToday.AddDays(3);
        var sessions = await GetRangeAsync(admin, future, future);
        var sessionId = sessions.Single(entry => entry.ClassId == classId).Id;

        var response = await student.PostJsonAsync(
            "/api/attendances/check-in", new { classSessionId = sessionId });

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Debt_within_the_tolerance_only_warns()
    {
        var admin = await CreateAdminClientAsync();
        var (student, studentId) = await CreateStudentWithAccessAsync(admin);
        var enrollmentId = await CreateEnrollmentAsync(
            admin, studentId, await CreatePlanAsync(admin), 200m, GymToday.AddDays(-2));

        await CreatePaymentAsync(admin, enrollmentId);

        var eligibility = await GetEligibilityAsync(student);

        eligibility.CanCheckIn.ShouldBeTrue();
        eligibility.Reason.ShouldBe(CheckInBlockReason.Ok);
        eligibility.HasOverdueDebt.ShouldBeTrue();
        eligibility.DaysOverdue.ShouldBe(2);
        eligibility.OverdueAmount.ShouldBe(200m);
        eligibility.OverdueCount.ShouldBe(1);
        eligibility.GraceDays.ShouldBe(5);
        eligibility.GraceDaysRemaining.ShouldBe(3);

        var classId = await CreateAllDayClassAsync(admin);
        var sessionId = (await GetTodayAsync(admin)).Single(entry => entry.ClassId == classId).Id;

        var checkedIn = await student.PostJsonAsync(
            "/api/attendances/check-in", new { classSessionId = sessionId });

        checkedIn.StatusCode.ShouldBe(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Debt_beyond_the_tolerance_blocks_the_check_in()
    {
        var admin = await CreateAdminClientAsync();
        var (student, studentId) = await CreateStudentWithAccessAsync(admin);
        var enrollmentId = await CreateEnrollmentAsync(
            admin, studentId, await CreatePlanAsync(admin), 150m, GymToday.AddDays(-20));

        await CreatePaymentAsync(admin, enrollmentId);

        var eligibility = await GetEligibilityAsync(student);

        eligibility.CanCheckIn.ShouldBeFalse();
        eligibility.Reason.ShouldBe(CheckInBlockReason.OverdueLimitExceeded);
        eligibility.DaysOverdue.ShouldBe(20);
        eligibility.GraceDaysRemaining.ShouldBe(0);

        var classId = await CreateAllDayClassAsync(admin);
        var sessionId = (await GetTodayAsync(admin)).Single(entry => entry.ClassId == classId).Id;

        // A regra é do servidor: mesmo chamando a API direto, o check-in é recusado.
        var response = await student.PostJsonAsync(
            "/api/attendances/check-in", new { classSessionId = sessionId });

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await response.Content.ReadAsStringAsync()).ShouldContain("atraso");
    }

    [Fact]
    public async Task Paying_the_debt_releases_the_check_in()
    {
        var admin = await CreateAdminClientAsync();
        var (student, studentId) = await CreateStudentWithAccessAsync(admin);
        var enrollmentId = await CreateEnrollmentAsync(
            admin, studentId, await CreatePlanAsync(admin), 150m, GymToday.AddDays(-20));

        var paymentId = await CreatePaymentAsync(admin, enrollmentId);

        (await GetEligibilityAsync(student)).CanCheckIn.ShouldBeFalse();

        await admin.PatchJsonAsync($"/api/payments/{paymentId}/settle", new { paydAt = (string?)null });

        var eligibility = await GetEligibilityAsync(student);
        eligibility.CanCheckIn.ShouldBeTrue();
        eligibility.HasOverdueDebt.ShouldBeFalse();
        eligibility.OverdueAmount.ShouldBe(0m);
    }

    [Fact]
    public async Task A_student_without_an_enrollment_cannot_train()
    {
        var admin = await CreateAdminClientAsync();
        var (student, _) = await CreateStudentWithAccessAsync(admin);

        var eligibility = await GetEligibilityAsync(student);

        eligibility.CanCheckIn.ShouldBeFalse();
        eligibility.Reason.ShouldBe(CheckInBlockReason.NoActiveEnrollment);
    }

    [Fact]
    public async Task A_registration_awaiting_approval_cannot_train()
    {
        var admin = await CreateAdminClientAsync();
        var email = UniqueEmail("pendente");
        var studentId = await CreateStudentAsync(admin, email, Password, StudentStatus.PendingApproval);
        await CreateEnrollmentAsync(admin, studentId, await CreatePlanAsync(admin));

        var student = await CreateClientForAsync(email, Password);

        var eligibility = await GetEligibilityAsync(student);

        eligibility.CanCheckIn.ShouldBeFalse();
        eligibility.Reason.ShouldBe(CheckInBlockReason.InactiveStudent);
    }

    [Fact]
    public async Task The_manual_roll_call_ignores_the_time_window()
    {
        var (admin, _, studentId) = await SetupAsync();
        var classId = await CreateAllDayClassAsync(admin);

        var future = GymToday.AddDays(3);
        var sessionId = (await GetRangeAsync(admin, future, future)).Single(entry => entry.ClassId == classId).Id;

        // A gestão registra o que aconteceu; não é uma liberação de acesso.
        var response = await admin.PostJsonAsync("/api/attendances", new
        {
            studentId,
            classSessionId = sessionId
        });

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Attendance_history_is_private_to_each_student()
    {
        var (admin, student, studentId) = await SetupAsync();
        var sessionId = await TodaySessionAsync(admin, student);

        await student.PostJsonAsync("/api/attendances/check-in", new { classSessionId = sessionId });

        var mine = await (await student.GetAsync(new Uri("/api/attendances", UriKind.Relative)))
            .ReadAsync<PagedResult<AttendanceDto>>();

        mine.TotalCount.ShouldBe(1);
        mine.Items[0].StudentId.ShouldBe(studentId);
        mine.Items[0].RegisteredBy.ShouldBe(AttendanceOrigin.Student);

        var otherStudent = await CreateStudentAsync(admin);
        var forbidden = await student.GetAsync(
            new Uri($"/api/attendances?studentId={otherStudent}", UriKind.Relative));

        forbidden.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task The_frequency_summary_counts_by_modality()
    {
        var (admin, student, _) = await SetupAsync();
        var sessionId = await TodaySessionAsync(admin, student);

        await student.PostJsonAsync("/api/attendances/check-in", new { classSessionId = sessionId });

        var summary = await (await student.GetAsync(new Uri("/api/attendances/summary", UriKind.Relative)))
            .ReadAsync<AttendanceSummaryDto>();

        summary.TotalCheckIns.ShouldBe(1);
        summary.ByModality.ShouldContain(modality => modality.Modality == Modality.MuayThai);
        summary.CurrentStreakDays.ShouldBe(1);
        summary.LastCheckInAt.ShouldNotBeNull();
    }

    [Fact]
    public async Task An_attendance_registered_by_mistake_can_be_removed()
    {
        var (admin, student, _) = await SetupAsync();
        var sessionId = await TodaySessionAsync(admin, student);

        var created = await student.PostJsonAsync(
            "/api/attendances/check-in", new { classSessionId = sessionId });
        var attendanceId = (await created.ReadAsync<CreatedResponse>()).Id;

        var deleted = await admin.DeleteAsync(new Uri($"/api/attendances/{attendanceId}", UriKind.Relative));
        deleted.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        // Removida a presença, o aluno pode bater ponto de novo.
        var again = await student.PostJsonAsync(
            "/api/attendances/check-in", new { classSessionId = sessionId });
        again.StatusCode.ShouldBe(HttpStatusCode.Created);
    }

    [Fact]
    public async Task The_session_range_is_capped()
    {
        var admin = await CreateAdminClientAsync();
        var from = GymToday;
        var to = from.AddDays(200);

        var response = await admin.GetAsync(
            new Uri($"/api/class-sessions?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Sessions_are_stable_between_requests()
    {
        var admin = await CreateAdminClientAsync();
        var classId = await CreateAllDayClassAsync(admin);

        var first = (await GetTodayAsync(admin)).Single(entry => entry.ClassId == classId).Id;
        var second = (await GetTodayAsync(admin)).Single(entry => entry.ClassId == classId).Id;

        // O id precisa ser estável, senão o check-in apontaria para uma ocorrência diferente a cada tela.
        second.ShouldBe(first);
    }

    private async Task<(HttpClient Admin, HttpClient Student, Guid StudentId)> SetupAsync()
    {
        var admin = await CreateAdminClientAsync();
        var (student, studentId) = await CreateStudentWithAccessAsync(admin);

        await CreateEnrollmentAsync(
            admin, studentId, await CreatePlanAsync(admin), 200m, GymToday.AddMonths(1));

        return (admin, student, studentId);
    }

    private async Task<(HttpClient Client, Guid StudentId)> CreateStudentWithAccessAsync(HttpClient admin)
    {
        var email = UniqueEmail("aluno");
        var studentId = await CreateStudentAsync(admin, email, Password);

        return (await CreateClientForAsync(email, Password), studentId);
    }

    /// <summary>
    /// Aula que cobre o dia inteiro, todos os dias: a janela de check-in está sempre aberta,
    /// então o teste não depende da hora em que roda. Cada aula ganha um professor próprio para
    /// não esbarrar na verificação de choque de horário.
    /// </summary>
    private static async Task<Guid> CreateAllDayClassAsync(HttpClient admin, int? capacity = null) =>
        await CreateClassAsync(
            admin, await CreateTeacherAsync(admin), [0, 1, 2, 3, 4, 5, 6], "00:00", "23:59", capacity);

    private static async Task<IReadOnlyList<ClassSessionDto>> GetTodayAsync(HttpClient client) =>
        await (await client.GetAsync(new Uri("/api/class-sessions/today", UriKind.Relative)))
            .ReadAsync<IReadOnlyList<ClassSessionDto>>();

    private static async Task<IReadOnlyList<ClassSessionDto>> GetRangeAsync(
        HttpClient client,
        DateOnly from,
        DateOnly to) =>
        await (await client.GetAsync(new Uri(
                $"/api/class-sessions?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}", UriKind.Relative)))
            .ReadAsync<IReadOnlyList<ClassSessionDto>>();

    private static async Task<CheckInEligibilityDto> GetEligibilityAsync(HttpClient student) =>
        await (await student.GetAsync(new Uri("/api/attendances/eligibility", UriKind.Relative)))
            .ReadAsync<CheckInEligibilityDto>();

    private static async Task<Guid> TodaySessionAsync(HttpClient admin, HttpClient student)
    {
        var classId = await CreateAllDayClassAsync(admin);

        return (await GetTodayAsync(student)).Single(entry => entry.ClassId == classId).Id;
    }
}
