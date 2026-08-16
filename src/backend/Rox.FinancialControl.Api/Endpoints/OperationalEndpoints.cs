using Microsoft.EntityFrameworkCore;
using Rox.FinancialControl.Infrastructure.Persistence;

namespace Rox.FinancialControl.Api.Endpoints;

public static class OperationalEndpoints
{
    public static IEndpointRouteBuilder MapOperationalEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/health", () => Results.Ok(new
        {
            status = "Healthy",
            checkedAt = DateTimeOffset.UtcNow
        }))
        .WithTags("Operations")
        .WithName("Health");

        app.MapGet("/api/operations/outbox", async (
            ApplicationDbContext dbContext,
            CancellationToken cancellationToken) =>
        {
            var totalPending = await dbContext.OutboxMessages
                .CountAsync(message => message.ProcessedAt == null, cancellationToken);

            var lastErrors = await dbContext.OutboxMessages
                .AsNoTracking()
                .Where(message => message.Error != null)
                .OrderByDescending(message => message.OccurredAt)
                .Take(5)
                .Select(message => new
                {
                    message.Id,
                    message.Type,
                    message.Attempts,
                    message.Error,
                    message.OccurredAt
                })
                .ToArrayAsync(cancellationToken);

            return Results.Ok(new
            {
                totalPending,
                lastErrors
            });
        })
        .WithTags("Operations")
        .WithName("GetOutboxStatus")
        .WithSummary("Shows outbox publication status.");

        return app;
    }
}
