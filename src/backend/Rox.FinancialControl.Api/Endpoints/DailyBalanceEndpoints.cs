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
            GetDailyBalanceHandler handler,
            CancellationToken cancellationToken) =>
        {
            var response = await handler.HandleAsync(businessDate, cancellationToken);

            return Results.Ok(response);
        })
        .WithName("GetDailyBalance")
        .WithSummary("Gets the consolidated balance for a business date.");

        group.MapGet("", async (
            DateOnly? from,
            DateOnly? to,
            ListDailyBalancesHandler handler,
            CancellationToken cancellationToken) =>
        {
            var response = await handler.HandleAsync(from, to, cancellationToken);

            return Results.Ok(response);
        })
        .WithName("ListDailyBalances")
        .WithSummary("Lists consolidated daily balances.");

        return group;
    }
}
