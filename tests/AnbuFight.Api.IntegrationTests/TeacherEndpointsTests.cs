using AnbuFight.Application.Common.Models;
using AnbuFight.Application.Teachers;

namespace AnbuFight.Api.IntegrationTests;

[Collection(nameof(ApiCollection))]
public class TeacherEndpointsTests(AnbuFightApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task Full_crud_cycle()
    {
        var client = await CreateAdminClientAsync();
        var email = UniqueEmail("teacher");

        var id = await CreateTeacherAsync(client, email);

        var created = await (await client.GetAsync(new Uri($"/api/teachers/{id}", UriKind.Relative)))
            .ReadAsync<TeacherDto>();

        created.Email.ShouldBe(email);
        created.FullName.ShouldBe("Hayate Mochizuki");
        created.Role.ShouldBe(UserRole.Teacher);

        var updated = await client.PutJsonAsync($"/api/teachers/{id}", new
        {
            firstName = "Hayate",
            lastName = "Kasumi",
            birthdate = "1988-02-20",
            email,
            phoneNumber = "+5511966666666",
            role = nameof(UserRole.Teacher)
        });

        updated.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var afterUpdate = await (await client.GetAsync(new Uri($"/api/teachers/{id}", UriKind.Relative)))
            .ReadAsync<TeacherDto>();

        afterUpdate.LastName.ShouldBe("Kasumi");
        afterUpdate.PhoneNumber.ShouldBe("+5511966666666");

        var deleted = await client.DeleteAsync(new Uri($"/api/teachers/{id}", UriKind.Relative));
        deleted.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        (await client.GetAsync(new Uri($"/api/teachers/{id}", UriKind.Relative)))
            .StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Create_with_a_password_lets_the_teacher_sign_in()
    {
        var client = await CreateAdminClientAsync();
        var email = UniqueEmail("teacher");

        var id = await CreateTeacherAsync(client, email, "Anbu@Fight123");

        var session = await SignInAsync(CreateAnonymousClient(), email, "Anbu@Fight123");

        session.User.Role.ShouldBe(UserRole.Teacher);
        session.User.TeacherId.ShouldBe(id);
    }

    [Fact]
    public async Task Create_rejects_a_duplicated_email()
    {
        var client = await CreateAdminClientAsync();
        var email = UniqueEmail("teacher");

        await CreateTeacherAsync(client, email);

        var response = await client.PostJsonAsync("/api/teachers", new
        {
            firstName = "Another",
            lastName = "Teacher",
            birthdate = "1990-01-01",
            email,
            phoneNumber = "+5511955555555",
            role = nameof(UserRole.Teacher)
        });

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task List_is_searchable()
    {
        var client = await CreateAdminClientAsync();
        var marker = $"marker{Guid.NewGuid():N}";

        await CreateTeacherAsync(client, $"{marker}@anbufight.com");

        var page = await (await client.GetAsync(new Uri($"/api/teachers?search={marker}", UriKind.Relative)))
            .ReadAsync<PagedResult<TeacherDto>>();

        page.TotalCount.ShouldBe(1);
        page.Items[0].Email.ShouldBe($"{marker}@anbufight.com");
    }
}
