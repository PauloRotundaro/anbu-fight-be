using AnbuFight.Application.ClassSessions;
using AnbuFight.Application.ClassSessions.Commands;
using AnbuFight.Application.ClassSessions.Queries;

namespace AnbuFight.Api.Endpoints;

/// <summary>Ocorrências concretas das aulas — o alvo do check-in.</summary>
public static class ClassSessionEndpoints
{
    public static IEndpointRouteBuilder MapClassSessionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/class-sessions").WithTags("ClassSessions");

        group.MapGet("/today", async (ISender sender, CancellationToken cancellationToken) =>
                TypedResults.Ok(await sender.Send(new GetTodayClassSessionsQuery(), cancellationToken)))
            .WithSummary("Aulas de hoje, já indicando se o aluno logado pode bater ponto em cada uma.")
            .Produces<IReadOnlyList<ClassSessionDto>>();

        group.MapGet("/", async (
                DateOnly? from,
                DateOnly? to,
                Modality? modality,
                Guid? teacherId,
                ISender sender,
                CancellationToken cancellationToken) =>
                TypedResults.Ok(await sender.Send(
                    new GetClassSessionsQuery(from, to, modality, teacherId), cancellationToken)))
            .WithSummary("Ocorrências num período. Sem parâmetros, devolve os próximos sete dias.")
            .Produces<IReadOnlyList<ClassSessionDto>>()
            .ProducesValidationProblem();

        group.MapGet("/{id:guid}", async (Guid id, ISender sender, CancellationToken cancellationToken) =>
                TypedResults.Ok(await sender.Send(new GetClassSessionByIdQuery(id), cancellationToken)))
            .WithSummary("Detalhe de uma ocorrência.")
            .Produces<ClassSessionDto>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPatch("/{id:guid}/cancel", async (
                Guid id,
                CancelClassSessionRequest? request,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                await sender.Send(new CancelClassSessionCommand(id, request?.Reason), cancellationToken);
                return TypedResults.NoContent();
            })
            .RequireAuthorization(Policies.Admin)
            .WithSummary("Cancela uma ocorrência (feriado, imprevisto) sem alterar a grade.")
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return app;
    }
}

/// <summary>Corpo opcional: o motivo aparece para o aluno na lista de aulas.</summary>
public sealed record CancelClassSessionRequest(string? Reason);
