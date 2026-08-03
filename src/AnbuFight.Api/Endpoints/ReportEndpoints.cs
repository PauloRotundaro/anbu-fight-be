using AnbuFight.Application.Reports;

namespace AnbuFight.Api.Endpoints;

public static class ReportEndpoints
{
    public static IEndpointRouteBuilder MapReportEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/reports").WithTags("Reports");

        group.MapGet("/financial-summary", async (
                DateOnly? from,
                DateOnly? to,
                ISender sender,
                CancellationToken cancellationToken) =>
                TypedResults.Ok(await sender.Send(
                    new GetFinancialSummaryQuery(from, to), cancellationToken)))
            .RequireAuthorization(Policies.Staff)
            .WithSummary("Faturado, recebido, em aberto, inadimplência e evolução mensal. Padrão: mês atual.")
            .Produces<FinancialSummaryDto>()
            .ProducesValidationProblem();

        return app;
    }
}
