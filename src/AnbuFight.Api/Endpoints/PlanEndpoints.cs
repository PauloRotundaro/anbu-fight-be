using AnbuFight.Api.Infrastructure;
using AnbuFight.Application.Plans;
using AnbuFight.Application.Plans.Commands;
using AnbuFight.Application.Plans.Queries;

namespace AnbuFight.Api.Endpoints;

public static class PlanEndpoints
{
    public static IEndpointRouteBuilder MapPlanEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/plans").WithTags("Plans");

        group.MapGet("/", async (
                string? search,
                PlanType? type,
                int? page,
                int? pageSize,
                ISender sender,
                CancellationToken cancellationToken) =>
                TypedResults.Ok(await sender.Send(
                    new GetPlansQuery(search, type, page ?? PagingDefaults.Page, pageSize ?? PagingDefaults.PageSize),
                    cancellationToken)))
            .WithSummary("Lists plans, paged, optionally filtered by name or recurrence.")
            .Produces<PagedResult<PlanDto>>();

        group.MapGet("/{id:guid}", async (Guid id, ISender sender, CancellationToken cancellationToken) =>
                TypedResults.Ok(await sender.Send(new GetPlanByIdQuery(id), cancellationToken)))
            .WithSummary("Gets a single plan.")
            .Produces<PlanDto>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/", async (CreatePlanCommand command, ISender sender, CancellationToken cancellationToken) =>
            {
                var id = await sender.Send(command, cancellationToken);
                return TypedResults.Created($"/api/plans/{id}", new CreatedResponse(id));
            })
            .RequireAuthorization(Policies.Admin)
            .WithSummary("Creates a plan.")
            .Produces<CreatedResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPut("/{id:guid}", async (
                Guid id,
                UpdatePlanRequest request,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                await sender.Send(
                    new UpdatePlanCommand(id, request.Name, request.Type, request.DefaultValue),
                    cancellationToken);
                return TypedResults.NoContent();
            })
            .RequireAuthorization(Policies.Admin)
            .WithSummary("Updates a plan.")
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapDelete("/{id:guid}", async (Guid id, ISender sender, CancellationToken cancellationToken) =>
            {
                await sender.Send(new DeletePlanCommand(id), cancellationToken);
                return TypedResults.NoContent();
            })
            .RequireAuthorization(Policies.Admin)
            .WithSummary("Soft deletes a plan that has no enrollments.")
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return app;
    }
}

public sealed record UpdatePlanRequest(string Name, PlanType Type, decimal? DefaultValue);
