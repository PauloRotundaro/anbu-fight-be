using AnbuFight.Api.Infrastructure;
using AnbuFight.Application.Attendances;
using AnbuFight.Application.Attendances.Commands;
using AnbuFight.Application.Attendances.Queries;

namespace AnbuFight.Api.Endpoints;

public static class AttendanceEndpoints
{
    public static IEndpointRouteBuilder MapAttendanceEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/attendances").WithTags("Attendances");

        group.MapPost("/check-in", async (
                CheckInCommand command,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var id = await sender.Send(command, cancellationToken);
                return TypedResults.Created($"/api/attendances/{id}", new CreatedResponse(id));
            })
            .WithSummary("Check-in do próprio aluno. Todas as regras são validadas no servidor.")
            .Produces<CreatedResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapGet("/eligibility", async (
                Guid? studentId,
                ISender sender,
                CancellationToken cancellationToken) =>
                TypedResults.Ok(await sender.Send(
                    new GetCheckInEligibilityQuery(studentId), cancellationToken)))
            .WithSummary("Se o aluno pode treinar agora, com o débito em atraso e a tolerância restante.")
            .Produces<CheckInEligibilityDto>()
            .ProducesProblem(StatusCodes.Status403Forbidden);

        group.MapGet("/summary", async (
                Guid? studentId,
                DateOnly? from,
                DateOnly? to,
                ISender sender,
                CancellationToken cancellationToken) =>
                TypedResults.Ok(await sender.Send(
                    new GetAttendanceSummaryQuery(studentId, from, to), cancellationToken)))
            .WithSummary("Resumo de frequência: total, por modalidade, sequência e média semanal.")
            .Produces<AttendanceSummaryDto>()
            .ProducesProblem(StatusCodes.Status403Forbidden);

        group.MapGet("/", async (
                Guid? studentId,
                Guid? classSessionId,
                Modality? modality,
                DateOnly? from,
                DateOnly? to,
                int? page,
                int? pageSize,
                ISender sender,
                CancellationToken cancellationToken) =>
                TypedResults.Ok(await sender.Send(
                    new GetAttendancesQuery(
                        studentId,
                        classSessionId,
                        modality,
                        from,
                        to,
                        page ?? PagingDefaults.Page,
                        pageSize ?? PagingDefaults.PageSize),
                    cancellationToken)))
            .WithSummary("Histórico de presenças; o aluno só enxerga as próprias.")
            .Produces<PagedResult<AttendanceDto>>();

        group.MapPost("/", async (
                CreateAttendanceCommand command,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var id = await sender.Send(command, cancellationToken);
                return TypedResults.Created($"/api/attendances/{id}", new CreatedResponse(id));
            })
            .RequireAuthorization(Policies.Admin)
            .WithSummary("Lançamento manual da chamada, sem janela de horário nem regra de débito.")
            .Produces<CreatedResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapDelete("/{id:guid}", async (Guid id, ISender sender, CancellationToken cancellationToken) =>
            {
                await sender.Send(new DeleteAttendanceCommand(id), cancellationToken);
                return TypedResults.NoContent();
            })
            .RequireAuthorization(Policies.Admin)
            .WithSummary("Remove uma presença lançada por engano.")
            .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }
}
