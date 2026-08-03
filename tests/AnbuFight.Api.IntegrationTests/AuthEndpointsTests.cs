using AnbuFight.Application.Auth;

namespace AnbuFight.Api.IntegrationTests;

[Collection(nameof(ApiCollection))]
public class AuthEndpointsTests(AnbuFightApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task Login_returns_a_token_pair_for_the_seeded_administrator()
    {
        var session = await SignInAsync(
            CreateAnonymousClient(),
            AnbuFightApiFactory.AdminEmail,
            AnbuFightApiFactory.AdminPassword);

        session.AccessToken.ShouldNotBeNullOrWhiteSpace();
        session.RefreshToken.ShouldNotBeNullOrWhiteSpace();
        session.ExpiresAt.ShouldBeGreaterThan(DateTimeOffset.UtcNow);
        session.User.Role.ShouldBe(UserRole.Admin);
        session.User.Email.ShouldBe(AnbuFightApiFactory.AdminEmail);
    }

    [Fact]
    public async Task Login_is_case_insensitive_on_the_email()
    {
        var response = await CreateAnonymousClient().PostJsonAsync("/api/auth/login", new
        {
            email = AnbuFightApiFactory.AdminEmail.ToUpperInvariant(),
            password = AnbuFightApiFactory.AdminPassword
        });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Login_rejects_a_wrong_password()
    {
        var response = await CreateAnonymousClient().PostJsonAsync("/api/auth/login", new
        {
            email = AnbuFightApiFactory.AdminEmail,
            password = "definitely-not-the-password"
        });

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_answers_the_same_way_for_an_unknown_email()
    {
        var response = await CreateAnonymousClient().PostJsonAsync("/api/auth/login", new
        {
            email = UniqueEmail("ghost"),
            password = "whatever-password"
        });

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_rejects_an_empty_payload_with_a_validation_problem()
    {
        var response = await CreateAnonymousClient().PostJsonAsync("/api/auth/login", new
        {
            email = string.Empty,
            password = string.Empty
        });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Protected_endpoints_require_a_token()
    {
        var response = await CreateAnonymousClient().GetAsync(new Uri("/api/students", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Me_describes_the_signed_in_user()
    {
        var client = await CreateAdminClientAsync();

        var response = await client.GetAsync(new Uri("/api/auth/me", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var user = await response.ReadAsync<AuthenticatedUserDto>();
        user.Email.ShouldBe(AnbuFightApiFactory.AdminEmail);
        user.Role.ShouldBe(UserRole.Admin);
        user.StudentId.ShouldBeNull();
    }

    [Fact]
    public async Task Me_links_a_student_credential_to_its_student_record()
    {
        var adminClient = await CreateAdminClientAsync();
        var email = UniqueEmail("student");
        var studentId = await CreateStudentAsync(adminClient, email, "Anbu@Fight123");

        var studentClient = await CreateClientForAsync(email, "Anbu@Fight123");
        var response = await studentClient.GetAsync(new Uri("/api/auth/me", UriKind.Relative));

        var user = await response.ReadAsync<AuthenticatedUserDto>();
        user.Role.ShouldBe(UserRole.Student);
        user.StudentId.ShouldBe(studentId);
        user.DisplayName.ShouldBe("Ryu Hayabusa");
    }

    [Fact]
    public async Task Refresh_rotates_the_pair_and_burns_the_presented_token()
    {
        var client = CreateAnonymousClient();
        var session = await SignInAsync(client, AnbuFightApiFactory.AdminEmail, AnbuFightApiFactory.AdminPassword);

        var refreshed = await client.PostJsonAsync("/api/auth/refresh", new { refreshToken = session.RefreshToken });
        refreshed.StatusCode.ShouldBe(HttpStatusCode.OK);

        var newSession = await refreshed.ReadAsync<AuthenticationResult>();
        newSession.RefreshToken.ShouldNotBe(session.RefreshToken);

        // The old token cannot be replayed.
        var replay = await client.PostJsonAsync("/api/auth/refresh", new { refreshToken = session.RefreshToken });
        replay.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_rejects_an_unknown_token()
    {
        var response = await CreateAnonymousClient()
            .PostJsonAsync("/api/auth/refresh", new { refreshToken = "not-a-real-token" });

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Logout_revokes_the_refresh_token()
    {
        var adminClient = await CreateAdminClientAsync();
        var email = UniqueEmail("student");
        await CreateStudentAsync(adminClient, email, "Anbu@Fight123");

        var client = CreateAnonymousClient();
        var session = await SignInAsync(client, email, "Anbu@Fight123");
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", session.AccessToken);

        var logout = await client.PostJsonAsync("/api/auth/logout", new { refreshToken = session.RefreshToken });
        logout.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var refresh = await client.PostJsonAsync("/api/auth/refresh", new { refreshToken = session.RefreshToken });
        refresh.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}
