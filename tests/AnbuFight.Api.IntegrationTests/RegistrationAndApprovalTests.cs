using AnbuFight.Api.Infrastructure;
using AnbuFight.Application.Auth;
using AnbuFight.Application.Common.Models;
using AnbuFight.Application.Students;

namespace AnbuFight.Api.IntegrationTests;

[Collection(nameof(ApiCollection))]
public class RegistrationAndApprovalTests(AnbuFightApiFactory factory) : IntegrationTestBase(factory)
{
    private const string Password = "Anbu@Fight123";

    [Fact]
    public async Task A_visitor_can_register_and_the_account_starts_pending()
    {
        var email = UniqueEmail("novo");

        var id = await RegisterAsync(email);

        var session = await SignInAsync(CreateAnonymousClient(), email, Password);

        // Decisão de produto: o cadastro pendente autentica normalmente e a tela mostra
        // "em análise" — em vez de o usuário achar que errou a senha.
        session.User.StudentId.ShouldBe(id);
        session.User.Role.ShouldBe(UserRole.Student);
        session.User.IsActive.ShouldBeFalse();
        session.User.StudentStatus.ShouldBe(StudentStatus.PendingApproval);
    }

    [Fact]
    public async Task Registering_ignores_any_attempt_to_pick_a_privileged_role()
    {
        var email = UniqueEmail("esperto");

        // O corpo do registro não tem "role" nem "status": campos extras são ignorados na desserialização.
        var response = await CreateAnonymousClient().PostJsonAsync("/api/auth/register", new
        {
            firstName = "Alguém",
            lastName = "Esperto",
            birthdate = "1990-01-01",
            email,
            phoneNumber = "+5511999999999",
            password = Password,
            role = nameof(UserRole.Admin),
            status = nameof(StudentStatus.Active)
        });

        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        var session = await SignInAsync(CreateAnonymousClient(), email, Password);
        session.User.Role.ShouldBe(UserRole.Student);
        session.User.StudentStatus.ShouldBe(StudentStatus.PendingApproval);
    }

    [Fact]
    public async Task Registering_an_existing_email_is_a_conflict()
    {
        var email = UniqueEmail("repetido");
        await RegisterAsync(email);

        var response = await CreateAnonymousClient().PostJsonAsync("/api/auth/register", new
        {
            firstName = "Outro",
            lastName = "Aluno",
            birthdate = "1990-01-01",
            email,
            phoneNumber = "+5511999999999",
            password = Password
        });

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Registering_rejects_a_short_password()
    {
        var response = await CreateAnonymousClient().PostJsonAsync("/api/auth/register", new
        {
            firstName = "Aluno",
            lastName = "Teste",
            birthdate = "1990-01-01",
            email = UniqueEmail("curto"),
            phoneNumber = "+5511999999999",
            password = "1234"
        });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Pending_registrations_show_up_in_the_approval_queue()
    {
        var admin = await CreateAdminClientAsync();
        var email = UniqueEmail("fila");
        var id = await RegisterAsync(email);

        var pending = await (await admin.GetAsync(new Uri(
                $"/api/students?status={nameof(StudentStatus.PendingApproval)}&search={email}",
                UriKind.Relative)))
            .ReadAsync<PagedResult<StudentDto>>();

        pending.TotalCount.ShouldBe(1);
        pending.Items[0].Id.ShouldBe(id);
    }

    [Fact]
    public async Task Approving_activates_the_student()
    {
        var admin = await CreateAdminClientAsync();
        var email = UniqueEmail("aprovado");
        var id = await RegisterAsync(email);

        var approved = await admin.PatchAsync(new Uri($"/api/students/{id}/approve", UriKind.Relative), null);
        approved.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var student = await (await admin.GetAsync(new Uri($"/api/students/{id}", UriKind.Relative)))
            .ReadAsync<StudentDto>();

        student.Status.ShouldBe(StudentStatus.Active);

        var session = await SignInAsync(CreateAnonymousClient(), email, Password);
        session.User.IsActive.ShouldBeTrue();

        // Aprovar de novo não faz sentido.
        var again = await admin.PatchAsync(new Uri($"/api/students/{id}/approve", UriKind.Relative), null);
        again.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Rejecting_removes_the_registration_and_frees_the_email()
    {
        var admin = await CreateAdminClientAsync();
        var email = UniqueEmail("recusado");
        var id = await RegisterAsync(email);

        var rejected = await admin.PatchAsync(new Uri($"/api/students/{id}/reject", UriKind.Relative), null);
        rejected.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        (await admin.GetAsync(new Uri($"/api/students/{id}", UriKind.Relative)))
            .StatusCode.ShouldBe(HttpStatusCode.NotFound);

        var login = await CreateAnonymousClient().PostJsonAsync("/api/auth/login", new { email, password = Password });
        login.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

        // O e-mail volta a ficar livre: pode ter sido engano.
        var again = await RegisterAsync(email);
        again.ShouldNotBe(id);
    }

    [Fact]
    public async Task An_active_student_cannot_be_rejected()
    {
        var admin = await CreateAdminClientAsync();
        var id = await CreateStudentAsync(admin);

        var response = await admin.PatchAsync(new Uri($"/api/students/{id}/reject", UriKind.Relative), null);

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Only_administrators_approve_registrations()
    {
        var admin = await CreateAdminClientAsync();
        var email = UniqueEmail("pendente");
        var id = await RegisterAsync(email);

        var pendingStudent = await CreateClientForAsync(email, Password);

        var response = await pendingStudent.PatchAsync(
            new Uri($"/api/students/{id}/approve", UriKind.Relative), null);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);

        // O administrador continua conseguindo.
        (await admin.PatchAsync(new Uri($"/api/students/{id}/approve", UriKind.Relative), null))
            .StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    private async Task<Guid> RegisterAsync(string email)
    {
        var response = await CreateAnonymousClient().PostJsonAsync("/api/auth/register", new
        {
            firstName = "Novo",
            lastName = "Aluno",
            birthdate = "1990-01-01",
            email,
            phoneNumber = "+5511999999999",
            password = Password
        });

        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        return (await response.ReadAsync<CreatedResponse>()).Id;
    }
}
