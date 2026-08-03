using AnbuFight.Application.Common.Models;
using AnbuFight.Application.Students;

namespace AnbuFight.Api.IntegrationTests;

[Collection(nameof(ApiCollection))]
public class StudentEndpointsTests(AnbuFightApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task Full_crud_cycle()
    {
        var client = await CreateAdminClientAsync();
        var email = UniqueEmail("student");

        // Create
        var id = await CreateStudentAsync(client, email);

        // Read
        var created = await (await client.GetAsync(new Uri($"/api/students/{id}", UriKind.Relative)))
            .ReadAsync<StudentDto>();

        created.Id.ShouldBe(id);
        created.Email.ShouldBe(email);
        created.FullName.ShouldBe("Ryu Hayabusa");
        created.Status.ShouldBe(StudentStatus.Active);
        created.IsActive.ShouldBeTrue();
        created.Role.ShouldBe(UserRole.Student);
        created.HasPortalAccess.ShouldBeFalse();
        created.CreatedAt.ShouldNotBe(default);

        // Update
        var updated = await client.PutJsonAsync($"/api/students/{id}", new
        {
            firstName = "Ryu",
            lastName = "Hayabusa Jr",
            birthdate = "1995-06-15",
            email,
            phoneNumber = "+5511900000000",
            status = nameof(StudentStatus.Inactive),
            emergencyContact = "Ayane",
            emergencyPhoneNumber = "+5511911111111",
            role = nameof(UserRole.Student)
        });

        updated.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var afterUpdate = await (await client.GetAsync(new Uri($"/api/students/{id}", UriKind.Relative)))
            .ReadAsync<StudentDto>();

        afterUpdate.LastName.ShouldBe("Hayabusa Jr");
        afterUpdate.Status.ShouldBe(StudentStatus.Inactive);
        afterUpdate.IsActive.ShouldBeFalse();
        afterUpdate.EmergencyContact.ShouldBe("Ayane");
        afterUpdate.UpdatedAt.ShouldBeGreaterThanOrEqualTo(created.UpdatedAt);

        // Delete
        var deleted = await client.DeleteAsync(new Uri($"/api/students/{id}", UriKind.Relative));
        deleted.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var afterDelete = await client.GetAsync(new Uri($"/api/students/{id}", UriKind.Relative));
        afterDelete.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Create_with_a_password_grants_portal_access()
    {
        var client = await CreateAdminClientAsync();
        var email = UniqueEmail("student");

        var id = await CreateStudentAsync(client, email, "Anbu@Fight123");

        var student = await (await client.GetAsync(new Uri($"/api/students/{id}", UriKind.Relative)))
            .ReadAsync<StudentDto>();

        student.HasPortalAccess.ShouldBeTrue();

        var session = await SignInAsync(CreateAnonymousClient(), email, "Anbu@Fight123");
        session.User.StudentId.ShouldBe(id);
    }

    [Fact]
    public async Task Create_rejects_a_duplicated_email()
    {
        var client = await CreateAdminClientAsync();
        var email = UniqueEmail("student");

        await CreateStudentAsync(client, email);

        var response = await client.PostJsonAsync("/api/students", new
        {
            firstName = "Another",
            lastName = "Student",
            birthdate = "1999-01-01",
            email,
            phoneNumber = "+5511922222222",
            role = nameof(UserRole.Student)
        });

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Create_returns_a_validation_problem_for_invalid_input()
    {
        var client = await CreateAdminClientAsync();

        var response = await client.PostJsonAsync("/api/students", new
        {
            firstName = string.Empty,
            lastName = "Hayabusa",
            birthdate = "2500-01-01",
            email = "not-an-email",
            phoneNumber = string.Empty,
            role = nameof(UserRole.Student)
        });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        var problem = await response.Content.ReadAsStringAsync();
        problem.ShouldContain("FirstName");
        problem.ShouldContain("Email");
    }

    [Fact]
    public async Task Get_returns_not_found_for_an_unknown_id()
    {
        var client = await CreateAdminClientAsync();

        var response = await client.GetAsync(new Uri($"/api/students/{Guid.NewGuid()}", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task List_is_paged_and_searchable()
    {
        var client = await CreateAdminClientAsync();
        var marker = $"marker{Guid.NewGuid():N}";

        await CreateStudentAsync(client, $"{marker}.one@anbufight.com");
        await CreateStudentAsync(client, $"{marker}.two@anbufight.com");

        var page = await (await client.GetAsync(
                new Uri($"/api/students?search={marker}&page=1&pageSize=1", UriKind.Relative)))
            .ReadAsync<PagedResult<StudentDto>>();

        page.Items.Count.ShouldBe(1);
        page.TotalCount.ShouldBe(2);
        page.TotalPages.ShouldBe(2);
        page.HasNextPage.ShouldBeTrue();
        page.HasPreviousPage.ShouldBeFalse();
    }

    [Fact]
    public async Task List_filters_by_status()
    {
        var client = await CreateAdminClientAsync();
        var marker = $"marker{Guid.NewGuid():N}";

        await CreateStudentAsync(client, $"{marker}.active@anbufight.com");
        await CreateStudentAsync(client, $"{marker}.inactive@anbufight.com", status: StudentStatus.Inactive);

        var actives = await (await client.GetAsync(
                new Uri($"/api/students?search={marker}&status={nameof(StudentStatus.Active)}", UriKind.Relative)))
            .ReadAsync<PagedResult<StudentDto>>();

        actives.TotalCount.ShouldBe(1);
        actives.Items.ShouldAllBe(student => student.IsActive);
    }

    [Fact]
    public async Task List_rejects_a_page_size_beyond_the_ceiling()
    {
        var client = await CreateAdminClientAsync();

        var response = await client.GetAsync(new Uri("/api/students?pageSize=5000", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Deleting_a_student_revokes_its_portal_access()
    {
        var client = await CreateAdminClientAsync();
        var email = UniqueEmail("student");
        var id = await CreateStudentAsync(client, email, "Anbu@Fight123");

        await client.DeleteAsync(new Uri($"/api/students/{id}", UriKind.Relative));

        var login = await CreateAnonymousClient().PostJsonAsync("/api/auth/login", new
        {
            email,
            password = "Anbu@Fight123"
        });

        login.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task A_deleted_email_can_be_reused()
    {
        var client = await CreateAdminClientAsync();
        var email = UniqueEmail("student");

        var id = await CreateStudentAsync(client, email);
        await client.DeleteAsync(new Uri($"/api/students/{id}", UriKind.Relative));

        // The unique index is filtered on deleted_at, so the address is free again.
        var recreatedId = await CreateStudentAsync(client, email);

        recreatedId.ShouldNotBe(id);
    }
}
