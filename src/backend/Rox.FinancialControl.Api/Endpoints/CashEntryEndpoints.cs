using Rox.FinancialControl.Application.CashEntries;

namespace Rox.FinancialControl.Api.Endpoints;

public static class CashEntryEndpoints
{
    public static RouteGroupBuilder MapCashEntryEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/cash-entries")
            .WithTags("Cash entries");

        group.MapPost("", async (
            CreateCashEntryRequest request,
            CreateCashEntryHandler handler,
            CancellationToken cancellationToken) =>
        {
            var response = await handler.HandleAsync(request, cancellationToken);

            return Results.Created($"/api/cash-entries/{response.Id}", response);
        })
        .WithName("CreateCashEntry")
        .WithSummary("Registers a credit or debit cash entry.");

        group.MapGet("{id:guid}", async (
            Guid id,
            GetCashEntryHandler handler,
            CancellationToken cancellationToken) =>
        {
            var response = await handler.HandleAsync(id, cancellationToken);

            return Results.Ok(response);
        })
        .WithName("GetCashEntry")
        .WithSummary("Gets a cash entry by id.");

        group.MapGet("", async (
            DateOnly? from,
            DateOnly? to,
            string? type,
            string? origin,
            int? page,
            int? pageSize,
            ListCashEntriesHandler handler,
            CancellationToken cancellationToken) =>
        {
            var response = await handler.HandleAsync(
                from,
                to,
                type,
                origin,
                page ?? 1,
                pageSize ?? 20,
                cancellationToken);

            return Results.Ok(response);
        })
        .WithName("ListCashEntries")
        .WithSummary("Lists cash entries with optional filters.");

        return group;
    }
}
