using AnbuFight.Application.Classes;
using AnbuFight.Application.Common.Models;

namespace AnbuFight.Api.IntegrationTests;

[Collection(nameof(ApiCollection))]
public class ClassEndpointsTests(AnbuFightApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task Full_crud_cycle()
    {
        var client = await CreateAdminClientAsync();
        var teacherId = await CreateTeacherAsync(client);

        var id = await CreateClassAsync(client, teacherId, [1, 3], "19:00", "20:00", capacity: 20);

        var created = await (await client.GetAsync(new Uri($"/api/classes/{id}", UriKind.Relative)))
            .ReadAsync<ClassDto>();

        created.Modality.ShouldBe(Modality.MuayThai);
        created.DaysOfWeek.ShouldBe([1, 3]);
        created.StartTime.ShouldBe(new TimeOnly(19, 0));
        created.EndTime.ShouldBe(new TimeOnly(20, 0));
        created.Capacity.ShouldBe(20);
        created.TeacherName.ShouldBe("Hayate Mochizuki");
        created.IsActive.ShouldBeTrue();

        var updated = await client.PutJsonAsync($"/api/classes/{id}", new
        {
            name = "Boxe Avançado",
            modality = nameof(Modality.Boxing),
            daysOfWeek = new[] { 2, 4 },
            startTime = "07:00",
            endTime = "08:00",
            teacherId,
            capacity = (int?)null,
            isActive = false
        });

        updated.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var afterUpdate = await (await client.GetAsync(new Uri($"/api/classes/{id}", UriKind.Relative)))
            .ReadAsync<ClassDto>();

        afterUpdate.Name.ShouldBe("Boxe Avançado");
        afterUpdate.Modality.ShouldBe(Modality.Boxing);
        afterUpdate.DaysOfWeek.ShouldBe([2, 4]);
        afterUpdate.Capacity.ShouldBeNull();
        afterUpdate.IsActive.ShouldBeFalse();

        var deleted = await client.DeleteAsync(new Uri($"/api/classes/{id}", UriKind.Relative));
        deleted.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        (await client.GetAsync(new Uri($"/api/classes/{id}", UriKind.Relative)))
            .StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task A_teacher_cannot_have_two_classes_at_the_same_time()
    {
        var client = await CreateAdminClientAsync();
        var teacherId = await CreateTeacherAsync(client);

        await CreateClassAsync(client, teacherId, [1, 3], "19:00", "20:00");

        var response = await client.PostJsonAsync("/api/classes", new
        {
            name = "Choque de horário",
            modality = nameof(Modality.Boxing),
            daysOfWeek = new[] { 3 },
            startTime = "19:30",
            endTime = "20:30",
            teacherId
        });

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task The_same_time_on_another_day_is_fine()
    {
        var client = await CreateAdminClientAsync();
        var teacherId = await CreateTeacherAsync(client);

        await CreateClassAsync(client, teacherId, [1], "19:00", "20:00");

        var response = await client.PostJsonAsync("/api/classes", new
        {
            name = "Outro dia",
            modality = nameof(Modality.Boxing),
            daysOfWeek = new[] { 2 },
            startTime = "19:00",
            endTime = "20:00",
            teacherId
        });

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
    }

    [Fact]
    public async Task The_end_must_come_after_the_start()
    {
        var client = await CreateAdminClientAsync();

        var response = await client.PostJsonAsync("/api/classes", new
        {
            name = "Invertida",
            modality = nameof(Modality.Boxing),
            daysOfWeek = new[] { 1 },
            startTime = "20:00",
            endTime = "19:00",
            teacherId = await CreateTeacherAsync(client)
        });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData(new int[] { })]
    [InlineData(new[] { 7 })]
    [InlineData(new[] { 1, 1 })]
    public async Task The_days_of_week_are_validated(int[] daysOfWeek)
    {
        var client = await CreateAdminClientAsync();

        var response = await client.PostJsonAsync("/api/classes", new
        {
            name = "Dias inválidos",
            modality = nameof(Modality.Boxing),
            daysOfWeek,
            startTime = "19:00",
            endTime = "20:00",
            teacherId = await CreateTeacherAsync(client)
        });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Creating_reports_an_unknown_teacher()
    {
        var client = await CreateAdminClientAsync();

        var response = await client.PostJsonAsync("/api/classes", new
        {
            name = "Sem professor",
            modality = nameof(Modality.Boxing),
            daysOfWeek = new[] { 1 },
            startTime = "19:00",
            endTime = "20:00",
            teacherId = Guid.NewGuid()
        });

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task The_schedule_can_be_filtered_by_day_teacher_and_modality()
    {
        var client = await CreateAdminClientAsync();
        var teacherId = await CreateTeacherAsync(client);

        await CreateClassAsync(client, teacherId, [5], "06:00", "07:00", modality: Modality.Boxing);

        var byTeacher = await (await client.GetAsync(
                new Uri($"/api/classes?teacherId={teacherId}&dayOfWeek=5", UriKind.Relative)))
            .ReadAsync<PagedResult<ClassDto>>();

        byTeacher.TotalCount.ShouldBe(1);
        byTeacher.Items[0].Modality.ShouldBe(Modality.Boxing);

        var otherDay = await (await client.GetAsync(
                new Uri($"/api/classes?teacherId={teacherId}&dayOfWeek=6", UriKind.Relative)))
            .ReadAsync<PagedResult<ClassDto>>();

        otherDay.TotalCount.ShouldBe(0);
    }

    [Fact]
    public async Task A_student_reads_the_schedule_but_does_not_change_it()
    {
        var admin = await CreateAdminClientAsync();
        var email = UniqueEmail("aluno");
        await CreateStudentAsync(admin, email, "Anbu@Fight123");

        var student = await CreateClientForAsync(email, "Anbu@Fight123");

        (await student.GetAsync(new Uri("/api/classes", UriKind.Relative)))
            .StatusCode.ShouldBe(HttpStatusCode.OK);

        var write = await student.PostJsonAsync("/api/classes", new
        {
            name = "Não pode",
            modality = nameof(Modality.Boxing),
            daysOfWeek = new[] { 1 },
            startTime = "19:00",
            endTime = "20:00",
            teacherId = await CreateTeacherAsync(admin)
        });

        write.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }
}
