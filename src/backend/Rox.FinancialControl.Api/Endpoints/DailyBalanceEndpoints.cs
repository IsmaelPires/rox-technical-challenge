using Rox.FinancialControl.Application.Balances;

namespace Rox.FinancialControl.Api.Endpoints;

public static class DailyBalanceEndpoints
{
    public static RouteGroupBuilder MapDailyBalanceEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/daily-balances")
            .WithTags("Daily balances");

        group.MapGet("{businessDate}", async (
            DateOnly businessDate,
            string? origin,
            GetDailyBalanceHandler handler,
            CancellationToken cancellationToken) =>
        {
            var response = await handler.HandleAsync(businessDate, origin, cancellationToken);

            return Results.Ok(response);
        })
        .WithName("GetDailyBalance")
        .WithSummary("Gets the consolidated balance for a business date.");

        group.MapGet("", async (
            DateOnly? from,
            DateOnly? to,
            string? origin,
            ListDailyBalancesHandler handler,
            CancellationToken cancellationToken) =>
        {
            var response = await handler.HandleAsync(from, to, origin, cancellationToken);

            return Results.Ok(response);
        })
        .WithName("ListDailyBalances")
        .WithSummary("Lists consolidated daily balances.");

        return group;
    }
}
