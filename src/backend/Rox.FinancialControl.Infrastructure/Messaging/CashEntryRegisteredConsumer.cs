using System.Data;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Rox.FinancialControl.Application.Abstractions;
using Rox.FinancialControl.Application.Messaging;
using Rox.FinancialControl.Domain.Balances;
using Rox.FinancialControl.Domain.Entries;
using Rox.FinancialControl.Infrastructure.Persistence;

namespace Rox.FinancialControl.Infrastructure.Messaging;

public sealed class CashEntryRegisteredConsumer(
    ApplicationDbContext dbContext,
    IClock clock,
    ILogger<CashEntryRegisteredConsumer> logger) : IConsumer<CashEntryRegisteredIntegrationEvent>
{
    public async Task Consume(ConsumeContext<CashEntryRegisteredIntegrationEvent> context)
    {
        var message = context.Message;

        if (!Enum.TryParse<CashEntryType>(message.Type, ignoreCase: true, out var entryType))
        {
            throw new InvalidOperationException($"Invalid cash entry type received: {message.Type}.");
        }

        var origin = Enum.TryParse<CashEntryOrigin>(message.Origin, ignoreCase: true, out var parsedOrigin)
            ? parsedOrigin
            : CashEntryOrigin.Business;

        var strategy = dbContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(
                IsolationLevel.Serializable,
                context.CancellationToken);

            await AcquireDailyBalanceLockAsync(message.BusinessDate, origin, context.CancellationToken);

            var alreadyProcessed = await dbContext.ProcessedCashEntries
                .AnyAsync(entry => entry.CashEntryId == message.CashEntryId, context.CancellationToken);

            if (alreadyProcessed)
            {
                logger.LogInformation(
                    "Cash entry {CashEntryId} was already consolidated. Skipping duplicate message.",
                    message.CashEntryId);

                await transaction.CommitAsync(context.CancellationToken);
                return;
            }

            var now = clock.UtcNow;
            var balance = await dbContext.DailyBalances
                .SingleOrDefaultAsync(
                    dailyBalance => dailyBalance.BusinessDate == message.BusinessDate
                        && dailyBalance.Origin == origin,
                    context.CancellationToken);

            if (balance is null)
            {
                balance = DailyBalance.Create(message.BusinessDate, origin, now);
                dbContext.DailyBalances.Add(balance);
            }

            balance.Apply(
                new CashEntrySnapshot(
                    message.CashEntryId,
                    message.BusinessDate,
                    entryType,
                    origin,
                    message.Amount),
                now);

            dbContext.ProcessedCashEntries.Add(new ProcessedCashEntry(message.CashEntryId, now));

            await dbContext.SaveChangesAsync(context.CancellationToken);
            await transaction.CommitAsync(context.CancellationToken);
        });
    }

    private async Task AcquireDailyBalanceLockAsync(
        DateOnly businessDate,
        CashEntryOrigin origin,
        CancellationToken cancellationToken)
    {
        const string lockMode = "Exclusive";
        const string lockOwner = "Transaction";
        const int lockTimeoutMilliseconds = 10_000;

        var lockName = $"daily-balance:{origin}:{businessDate:yyyy-MM-dd}";

        // Serialize updates for the same business date while allowing different dates to process in parallel.
        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"DECLARE @Result int; EXEC @Result = sp_getapplock @Resource = {lockName}, @LockMode = {lockMode}, @LockOwner = {lockOwner}, @LockTimeout = {lockTimeoutMilliseconds}; IF @Result < 0 THROW 51000, 'Could not acquire daily balance application lock.', 1;",
            cancellationToken);
    }
}
