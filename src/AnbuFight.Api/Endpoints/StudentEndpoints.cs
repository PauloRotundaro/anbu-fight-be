using AnbuFight.Api.Infrastructure;
using AnbuFight.Application.Auth.Commands;
using AnbuFight.Application.Students;
using AnbuFight.Application.Students.Commands;
using AnbuFight.Application.Students.Queries;

namespace AnbuFight.Api.Endpoints;

public static class StudentEndpoints
{
    public static IEndpointRouteBuilder MapStudentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/students").WithTags("Students");

        group.MapGet("/", async (
                string? search,
                StudentStatus? status,
                int? page,
                int? pageSize,
                ISender sender,
                CancellationToken cancellationToken) =>
                TypedResults.Ok(await sender.Send(
                    new GetStudentsQuery(
                        search,
                        status,
                        page ?? PagingDefaults.Page,
                        pageSize ?? PagingDefaults.PageSize),
                    cancellationToken)))
            .RequireAuthorization(Policies.Staff)
            .WithSummary("Lista alunos; filtre por status=PendingApproval para a fila de aprovação.")
            .Produces<PagedResult<StudentDto>>();

        group.MapGet("/birthdays", async (
                int? daysAhead,
                bool? onlyActive,
                ISender sender,
                CancellationToken cancellationToken) =>
                TypedResults.Ok(await sender.Send(
                    new GetBirthdaysQuery(daysAhead ?? 30, onlyActive ?? true),
                    cancellationToken)))
            .RequireAuthorization(Policies.Staff)
            .WithSummary("Aniversariantes dos próximos dias, ordenados por proximidade.")
            .Produces<IReadOnlyList<BirthdayStudentDto>>();

        // Qualquer usuário autenticado: o handler garante que um aluno só leia o próprio cadastro.
        group.MapGet("/{id:guid}", async (Guid id, ISender sender, CancellationToken cancellationToken) =>
                TypedResults.Ok(await sender.Send(new GetStudentByIdQuery(id), cancellationToken)))
            .WithSummary("Gets a single student.")
            .Produces<StudentDto>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/{id:guid}/summary", async (
                Guid id,
                int? recentItems,
                ISender sender,
                CancellationToken cancellationToken) =>
                TypedResults.Ok(await sender.Send(
                    new GetStudentSummaryQuery(id, recentItems ?? 5), cancellationToken)))
            .WithSummary("Aluno, matrículas, últimas cobranças e presenças numa resposta só.")
            .Produces<StudentSummaryDto>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPut("/me", async (
                UpdateMyProfileCommand command,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                await sender.Send(command, cancellationToken);
                return TypedResults.NoContent();
            })
            .WithSummary("O aluno atualiza os próprios dados de contato.")
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status403Forbidden);

        group.MapPost("/", async (
                CreateStudentCommand command,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var id = await sender.Send(command, cancellationToken);
                return TypedResults.Created($"/api/students/{id}", new CreatedResponse(id));
            })
            .RequireAuthorization(Policies.Admin)
            .WithSummary("Registers a student; pass a password to also create the sign-in credential.")
            .Produces<CreatedResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPut("/{id:guid}", async (
                Guid id,
                UpdateStudentRequest request,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                await sender.Send(request.ToCommand(id), cancellationToken);
                return TypedResults.NoContent();
            })
            .RequireAuthorization(Policies.Admin)
            .WithSummary("Updates a student.")
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPatch("/{id:guid}/approve", async (
                Guid id,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                await sender.Send(new ApproveStudentCommand(id), cancellationToken);
                return TypedResults.NoContent();
            })
            .RequireAuthorization(Policies.Admin)
            .WithSummary("Aprova um auto-cadastro pendente.")
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPatch("/{id:guid}/reject", async (
                Guid id,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                await sender.Send(new RejectStudentCommand(id), cancellationToken);
                return TypedResults.NoContent();
            })
            .RequireAuthorization(Policies.Admin)
            .WithSummary("Recusa um auto-cadastro pendente (exclusão lógica).")
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/{id:guid}/portal-access", async (
                Guid id,
                SetPortalAccessRequest request,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                await sender.Send(new SetStudentPortalAccessCommand(id, request.Password), cancellationToken);
                return TypedResults.NoContent();
            })
            .RequireAuthorization(Policies.Admin)
            .WithSummary("Cria o login do aluno ou redefine a senha dele.")
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapDelete("/{id:guid}", async (Guid id, ISender sender, CancellationToken cancellationToken) =>
            {
                await sender.Send(new DeleteStudentCommand(id), cancellationToken);
                return TypedResults.NoContent();
            })
            .RequireAuthorization(Policies.Admin)
            .WithSummary("Soft deletes a student and revokes its access.")
            .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }
}

/// <summary>Update payload; the id comes from the route, never from the body.</summary>
public sealed record UpdateStudentRequest(
    string FirstName,
    string LastName,
    DateOnly Birthdate,
    string Email,
    string PhoneNumber,
    StudentStatus Status,
    string? EmergencyContact,
    string? EmergencyPhoneNumber,
    UserRole Role)
{
    public UpdateStudentCommand ToCommand(Guid id) => new(
        id,
        FirstName,
        LastName,
        Birthdate,
        Email,
        PhoneNumber,
        Status,
        EmergencyContact,
        EmergencyPhoneNumber,
        Role);
}

public sealed record SetPortalAccessRequest(string Password);
