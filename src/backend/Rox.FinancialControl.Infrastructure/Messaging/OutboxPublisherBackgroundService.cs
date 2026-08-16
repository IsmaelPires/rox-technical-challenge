using System.Text.Json;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Rox.FinancialControl.Application.Abstractions;
using Rox.FinancialControl.Application.Messaging;
using Rox.FinancialControl.Infrastructure.Persistence;

namespace Rox.FinancialControl.Infrastructure.Messaging;

public sealed class OutboxPublisherBackgroundService(
    IServiceScopeFactory scopeFactory,
    IOptions<OutboxOptions> options,
    ILogger<OutboxPublisherBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var pollingInterval = TimeSpan.FromSeconds(Math.Max(1, options.Value.PollingIntervalSeconds));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PublishPendingMessagesAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error while publishing outbox messages.");
            }

            await Task.Delay(pollingInterval, stoppingToken);
        }
    }

    private async Task PublishPendingMessagesAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var publishEndpoint = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();
        var clock = scope.ServiceProvider.GetRequiredService<IClock>();
        var batchSize = Math.Clamp(options.Value.BatchSize, 1, 500);

        var messages = await dbContext.OutboxMessages
            .Where(message => message.ProcessedAt == null)
            .OrderBy(message => message.OccurredAt)
            .Take(batchSize)
            .ToListAsync(cancellationToken);

        foreach (var outboxMessage in messages)
        {
            try
            {
                var message = Deserialize(outboxMessage);
                await publishEndpoint.Publish(message, message.GetType(), cancellationToken);

                outboxMessage.MarkProcessed(clock.UtcNow);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Could not publish outbox message {MessageId}.", outboxMessage.Id);
                outboxMessage.MarkFailed(ex, clock.UtcNow);
            }
        }

        if (messages.Count > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private static object Deserialize(OutboxMessage outboxMessage)
    {
        if (outboxMessage.Type == typeof(CashEntryRegisteredIntegrationEvent).FullName)
        {
            return JsonSerializer.Deserialize<CashEntryRegisteredIntegrationEvent>(
                outboxMessage.Payload,
                MessagingJsonOptions.Instance)
                ?? throw new InvalidOperationException("Invalid cash entry event payload.");
        }

        throw new InvalidOperationException($"Unsupported outbox message type: {outboxMessage.Type}.");
    }
}
