using AnbuFight.Api.Infrastructure;
using AnbuFight.Application.Auth.Commands;
using AnbuFight.Application.Teachers;
using AnbuFight.Application.Teachers.Commands;
using AnbuFight.Application.Teachers.Queries;

namespace AnbuFight.Api.Endpoints;

public static class TeacherEndpoints
{
    public static IEndpointRouteBuilder MapTeacherEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/teachers").WithTags("Teachers");

        group.MapGet("/", async (
                string? search,
                int? page,
                int? pageSize,
                ISender sender,
                CancellationToken cancellationToken) =>
                TypedResults.Ok(await sender.Send(
                    new GetTeachersQuery(search, page ?? PagingDefaults.Page, pageSize ?? PagingDefaults.PageSize),
                    cancellationToken)))
            .WithSummary("Lists teachers, paged.")
            .Produces<PagedResult<TeacherDto>>();

        group.MapGet("/{id:guid}", async (Guid id, ISender sender, CancellationToken cancellationToken) =>
                TypedResults.Ok(await sender.Send(new GetTeacherByIdQuery(id), cancellationToken)))
            .WithSummary("Gets a single teacher.")
            .Produces<TeacherDto>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/", async (
                CreateTeacherCommand command,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var id = await sender.Send(command, cancellationToken);
                return TypedResults.Created($"/api/teachers/{id}", new CreatedResponse(id));
            })
            .RequireAuthorization(Policies.Admin)
            .WithSummary("Registers a teacher; pass a password to also create the sign-in credential.")
            .Produces<CreatedResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPut("/{id:guid}", async (
                Guid id,
                UpdateTeacherRequest request,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                await sender.Send(request.ToCommand(id), cancellationToken);
                return TypedResults.NoContent();
            })
            .RequireAuthorization(Policies.Admin)
            .WithSummary("Updates a teacher.")
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/{id:guid}/portal-access", async (
                Guid id,
                SetPortalAccessRequest request,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                await sender.Send(new SetTeacherPortalAccessCommand(id, request.Password), cancellationToken);
                return TypedResults.NoContent();
            })
            .RequireAuthorization(Policies.Admin)
            .WithSummary("Cria o login do professor ou redefine a senha dele.")
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapDelete("/{id:guid}", async (Guid id, ISender sender, CancellationToken cancellationToken) =>
            {
                await sender.Send(new DeleteTeacherCommand(id), cancellationToken);
                return TypedResults.NoContent();
            })
            .RequireAuthorization(Policies.Admin)
            .WithSummary("Soft deletes a teacher and revokes its access.")
            .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }
}

public sealed record UpdateTeacherRequest(
    string FirstName,
    string LastName,
    DateOnly Birthdate,
    string Email,
    string PhoneNumber,
    UserRole Role)
{
    public UpdateTeacherCommand ToCommand(Guid id) =>
        new(id, FirstName, LastName, Birthdate, Email, PhoneNumber, Role);
}
