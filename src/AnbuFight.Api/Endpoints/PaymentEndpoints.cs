using AnbuFight.Api.Infrastructure;
using AnbuFight.Application.Payments;
using AnbuFight.Application.Payments.Commands;
using AnbuFight.Application.Payments.Queries;

namespace AnbuFight.Api.Endpoints;

public static class PaymentEndpoints
{
    public static IEndpointRouteBuilder MapPaymentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/payments").WithTags("Payments");

        group.MapGet("/", async (
                Guid? studentId,
                Guid? studentPlanId,
                PaymentStatus? status,
                DateOnly? dueFrom,
                DateOnly? dueTo,
                int? page,
                int? pageSize,
                ISender sender,
                CancellationToken cancellationToken) =>
                TypedResults.Ok(await sender.Send(
                    new GetPaymentsQuery(
                        studentId,
                        studentPlanId,
                        status,
                        dueFrom,
                        dueTo,
                        page ?? PagingDefaults.Page,
                        pageSize ?? PagingDefaults.PageSize),
                    cancellationToken)))
            .WithSummary("Lists charges; filter by status to get the receivables or the overdue list.")
            .Produces<PagedResult<PaymentDto>>();

        group.MapGet("/{id:guid}", async (Guid id, ISender sender, CancellationToken cancellationToken) =>
                TypedResults.Ok(await sender.Send(new GetPaymentByIdQuery(id), cancellationToken)))
            .WithSummary("Gets a single charge.")
            .Produces<PaymentDto>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/", async (
                CreatePaymentCommand command,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var id = await sender.Send(command, cancellationToken);
                return TypedResults.Created($"/api/payments/{id}", new CreatedResponse(id));
            })
            .RequireAuthorization(Policies.Admin)
            .WithSummary("Issues a charge against an enrollment.")
            .Produces<CreatedResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/bulk-generate", async (
                BulkGeneratePaymentsCommand command,
                ISender sender,
                CancellationToken cancellationToken) =>
                TypedResults.Ok(await sender.Send(command, cancellationToken)))
            .RequireAuthorization(Policies.Admin)
            .WithSummary("Emite as cobranças do mês para todas as matrículas que vencem no período.")
            .Produces<BulkGenerationResult>()
            .ProducesValidationProblem();

        group.MapPut("/{id:guid}", async (
                Guid id,
                UpdatePaymentRequest request,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                await sender.Send(new UpdatePaymentCommand(id, request.Value, request.DueDate), cancellationToken);
                return TypedResults.NoContent();
            })
            .RequireAuthorization(Policies.Admin)
            .WithSummary("Corrects an open charge.")
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPatch("/{id:guid}/settle", async (
                Guid id,
                SettlePaymentRequest? request,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                await sender.Send(new SettlePaymentCommand(id, request?.PaydAt), cancellationToken);
                return TypedResults.NoContent();
            })
            .RequireAuthorization(Policies.Admin)
            .WithSummary("Settles a charge and rolls the enrollment to the next billing cycle.")
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapDelete("/{id:guid}", async (Guid id, ISender sender, CancellationToken cancellationToken) =>
            {
                await sender.Send(new DeletePaymentCommand(id), cancellationToken);
                return TypedResults.NoContent();
            })
            .RequireAuthorization(Policies.Admin)
            .WithSummary("Soft deletes a charge.")
            .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }
}

public sealed record UpdatePaymentRequest(decimal Value, DateOnly DueDate);

/// <summary>Optional body: when omitted the charge is settled with the current instant.</summary>
public sealed record SettlePaymentRequest(DateTimeOffset? PaydAt);
