using AnbuFight.Api.Infrastructure;
using AnbuFight.Application.Classes;
using AnbuFight.Application.Classes.Commands;
using AnbuFight.Application.Classes.Queries;

namespace AnbuFight.Api.Endpoints;

/// <summary>Grade horária semanal da academia.</summary>
public static class ClassEndpoints
{
    public static IEndpointRouteBuilder MapClassEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/classes").WithTags("Classes");

        group.MapGet("/", async (
                Modality? modality,
                int? dayOfWeek,
                Guid? teacherId,
                bool? isActive,
                int? page,
                int? pageSize,
                ISender sender,
                CancellationToken cancellationToken) =>
                TypedResults.Ok(await sender.Send(
                    new GetClassesQuery(
                        modality,
                        dayOfWeek,
                        teacherId,
                        isActive,
                        page ?? PagingDefaults.Page,
                        pageSize ?? PagingDefaults.PageSize),
                    cancellationToken)))
            .WithSummary("Lista as aulas da grade, com filtros por modalidade, dia e professor.")
            .Produces<PagedResult<ClassDto>>();

        group.MapGet("/{id:guid}", async (Guid id, ISender sender, CancellationToken cancellationToken) =>
                TypedResults.Ok(await sender.Send(new GetClassByIdQuery(id), cancellationToken)))
            .WithSummary("Detalhe de uma aula.")
            .Produces<ClassDto>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/", async (
                CreateClassCommand command,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var id = await sender.Send(command, cancellationToken);
                return TypedResults.Created($"/api/classes/{id}", new CreatedResponse(id));
            })
            .RequireAuthorization(Policies.Admin)
            .WithSummary("Cria uma aula recorrente. 409 se o professor já tiver aula no mesmo horário.")
            .Produces<CreatedResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPut("/{id:guid}", async (
                Guid id,
                UpdateClassRequest request,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                await sender.Send(request.ToCommand(id), cancellationToken);
                return TypedResults.NoContent();
            })
            .RequireAuthorization(Policies.Admin)
            .WithSummary("Atualiza a aula. Sessões futuras sem presença são regeradas com o novo horário.")
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapDelete("/{id:guid}", async (Guid id, ISender sender, CancellationToken cancellationToken) =>
            {
                await sender.Send(new DeleteClassCommand(id), cancellationToken);
                return TypedResults.NoContent();
            })
            .RequireAuthorization(Policies.Admin)
            .WithSummary("Exclui a aula. 409 se já houver presenças registradas.")
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return app;
    }
}

public sealed record UpdateClassRequest(
    string Name,
    Modality Modality,
    IReadOnlyList<int> DaysOfWeek,
    TimeOnly StartTime,
    TimeOnly EndTime,
    Guid TeacherId,
    int? Capacity,
    bool IsActive)
{
    public UpdateClassCommand ToCommand(Guid id) =>
        new(id, Name, Modality, DaysOfWeek, StartTime, EndTime, TeacherId, Capacity, IsActive);
}
