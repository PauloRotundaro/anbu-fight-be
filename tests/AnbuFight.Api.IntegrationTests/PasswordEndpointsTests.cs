namespace AnbuFight.Api.IntegrationTests;

[Collection(nameof(ApiCollection))]
public class PasswordEndpointsTests(AnbuFightApiFactory factory) : IntegrationTestBase(factory)
{
    private const string Password = "Anbu@Fight123";
    private const string NewPassword = "Nova@Senha456";

    [Fact]
    public async Task A_user_can_change_the_own_password_and_sign_in_with_the_new_one()
    {
        var admin = await CreateAdminClientAsync();
        var email = UniqueEmail("troca");
        await CreateStudentAsync(admin, email, Password);

        var client = await CreateClientForAsync(email, Password);

        var changed = await client.PatchJsonAsync("/api/auth/password", new
        {
            currentPassword = Password,
            newPassword = NewPassword
        });

        changed.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var withNewPassword = await SignInAsync(CreateAnonymousClient(), email, NewPassword);
        withNewPassword.AccessToken.ShouldNotBeNullOrWhiteSpace();

        var withOldPassword = await CreateAnonymousClient()
            .PostJsonAsync("/api/auth/login", new { email, password = Password });

        withOldPassword.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Changing_the_password_kills_the_other_sessions()
    {
        var admin = await CreateAdminClientAsync();
        var email = UniqueEmail("sessoes");
        await CreateStudentAsync(admin, email, Password);

        // Duas sessões abertas, como dois dispositivos.
        var otherDevice = await SignInAsync(CreateAnonymousClient(), email, Password);
        var client = await CreateClientForAsync(email, Password);

        await client.PatchJsonAsync("/api/auth/password", new
        {
            currentPassword = Password,
            newPassword = NewPassword
        });

        var refresh = await CreateAnonymousClient()
            .PostJsonAsync("/api/auth/refresh", new { refreshToken = otherDevice.RefreshToken });

        refresh.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Changing_the_password_can_keep_the_current_session_alive()
    {
        var admin = await CreateAdminClientAsync();
        var email = UniqueEmail("mantem");
        await CreateStudentAsync(admin, email, Password);

        var anonymous = CreateAnonymousClient();
        var session = await SignInAsync(anonymous, email, Password);

        var client = await CreateClientForAsync(email, Password);

        await client.PatchJsonAsync("/api/auth/password", new
        {
            currentPassword = Password,
            newPassword = NewPassword,
            currentRefreshToken = session.RefreshToken
        });

        var refresh = await anonymous
            .PostJsonAsync("/api/auth/refresh", new { refreshToken = session.RefreshToken });

        refresh.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Changing_the_password_requires_the_current_one()
    {
        var admin = await CreateAdminClientAsync();
        var email = UniqueEmail("errada");
        await CreateStudentAsync(admin, email, Password);

        var client = await CreateClientForAsync(email, Password);

        var response = await client.PatchJsonAsync("/api/auth/password", new
        {
            currentPassword = "senha-errada",
            newPassword = NewPassword
        });

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task The_new_password_must_differ_from_the_current_one()
    {
        var admin = await CreateAdminClientAsync();
        var email = UniqueEmail("igual");
        await CreateStudentAsync(admin, email, Password);

        var client = await CreateClientForAsync(email, Password);

        var response = await client.PatchJsonAsync("/api/auth/password", new
        {
            currentPassword = Password,
            newPassword = Password
        });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData("nao.existe@anbufight.com")]
    [InlineData("admin@anbufight.com")]
    public async Task Forgot_password_answers_the_same_way_for_any_email(string email)
    {
        // Resposta idêntica nos dois casos: a rota é pública e não pode revelar quem tem cadastro.
        var response = await CreateAnonymousClient().PostJsonAsync("/api/auth/forgot-password", new { email });

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Reset_password_rejects_an_unknown_token()
    {
        var response = await CreateAnonymousClient().PostJsonAsync("/api/auth/reset-password", new
        {
            token = "token-que-nunca-existiu",
            newPassword = NewPassword
        });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Administrators_can_grant_portal_access_to_a_student_without_one()
    {
        var admin = await CreateAdminClientAsync();
        var email = UniqueEmail("sem-acesso");
        var id = await CreateStudentAsync(admin, email);

        var beforeAccess = await CreateAnonymousClient()
            .PostJsonAsync("/api/auth/login", new { email, password = Password });
        beforeAccess.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

        var granted = await admin.PostJsonAsync(
            $"/api/students/{id}/portal-access", new { password = Password });
        granted.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var session = await SignInAsync(CreateAnonymousClient(), email, Password);
        session.User.StudentId.ShouldBe(id);
    }

    [Fact]
    public async Task Resetting_a_password_through_the_gym_kills_the_open_sessions()
    {
        var admin = await CreateAdminClientAsync();
        var email = UniqueEmail("reset-gestao");
        var id = await CreateStudentAsync(admin, email, Password);

        var session = await SignInAsync(CreateAnonymousClient(), email, Password);

        await admin.PostJsonAsync($"/api/students/{id}/portal-access", new { password = NewPassword });

        var refresh = await CreateAnonymousClient()
            .PostJsonAsync("/api/auth/refresh", new { refreshToken = session.RefreshToken });
        refresh.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

        var withNewPassword = await SignInAsync(CreateAnonymousClient(), email, NewPassword);
        withNewPassword.AccessToken.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Teachers_also_get_portal_access_from_the_gym()
    {
        var admin = await CreateAdminClientAsync();
        var email = UniqueEmail("professor");
        var id = await CreateTeacherAsync(admin, email);

        var granted = await admin.PostJsonAsync(
            $"/api/teachers/{id}/portal-access", new { password = Password });
        granted.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var session = await SignInAsync(CreateAnonymousClient(), email, Password);
        session.User.TeacherId.ShouldBe(id);
    }

    [Fact]
    public async Task Only_administrators_grant_portal_access()
    {
        var admin = await CreateAdminClientAsync();
        var email = UniqueEmail("aluno");
        var id = await CreateStudentAsync(admin, email, Password);

        var student = await CreateClientForAsync(email, Password);

        var response = await student.PostJsonAsync(
            $"/api/students/{id}/portal-access", new { password = NewPassword });

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }
}
