using AnbuFight.Api.Infrastructure;
using AnbuFight.Application.StudentPlans;
using AnbuFight.Application.StudentPlans.Commands;
using AnbuFight.Application.StudentPlans.Queries;

namespace AnbuFight.Api.Endpoints;

/// <summary>Enrollments: the N-M link between students and plans.</summary>
public static class StudentPlanEndpoints
{
    public static IEndpointRouteBuilder MapStudentPlanEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/student-plans").WithTags("StudentPlans");

        group.MapGet("/", async (
                Guid? studentId,
                Guid? planId,
                int? page,
                int? pageSize,
                ISender sender,
                CancellationToken cancellationToken) =>
                TypedResults.Ok(await sender.Send(
                    new GetStudentPlansQuery(
                        studentId,
                        planId,
                        page ?? PagingDefaults.Page,
                        pageSize ?? PagingDefaults.PageSize),
                    cancellationToken)))
            .WithSummary("Lists enrollments; a signed-in student only ever sees its own.")
            .Produces<PagedResult<StudentPlanDto>>();

        group.MapGet("/{id:guid}", async (Guid id, ISender sender, CancellationToken cancellationToken) =>
                TypedResults.Ok(await sender.Send(new GetStudentPlanByIdQuery(id), cancellationToken)))
            .WithSummary("Gets a single enrollment.")
            .Produces<StudentPlanDto>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/", async (
                CreateStudentPlanCommand command,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var id = await sender.Send(command, cancellationToken);
                return TypedResults.Created($"/api/student-plans/{id}", new CreatedResponse(id));
            })
            .RequireAuthorization(Policies.Admin)
            .WithSummary("Enrolls a student in a plan.")
            .Produces<CreatedResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPut("/{id:guid}", async (
                Guid id,
                UpdateStudentPlanRequest request,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                await sender.Send(
                    new UpdateStudentPlanCommand(id, request.PlanValue, request.DueDate),
                    cancellationToken);

                return TypedResults.NoContent();
            })
            .RequireAuthorization(Policies.Admin)
            .WithSummary("Updates the negotiated price and the next due date.")
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapDelete("/{id:guid}", async (Guid id, ISender sender, CancellationToken cancellationToken) =>
            {
                await sender.Send(new DeleteStudentPlanCommand(id), cancellationToken);
                return TypedResults.NoContent();
            })
            .RequireAuthorization(Policies.Admin)
            .WithSummary("Soft deletes an enrollment that has no open payments.")
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return app;
    }
}

public sealed record UpdateStudentPlanRequest(decimal PlanValue, DateOnly DueDate);
