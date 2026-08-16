using System.Text.Json;
using Rox.FinancialControl.Application.Abstractions;
using Rox.FinancialControl.Infrastructure.Persistence;

namespace Rox.FinancialControl.Infrastructure.Messaging;

public sealed class OutboxWriter(ApplicationDbContext dbContext, IClock clock) : IOutboxWriter
{
    public Task AddAsync<TMessage>(TMessage message, CancellationToken cancellationToken)
        where TMessage : notnull
    {
        var payload = JsonSerializer.Serialize(message, MessagingJsonOptions.Instance);
        var outboxMessage = OutboxMessage.Create(typeof(TMessage).FullName!, payload, clock.UtcNow);

        dbContext.OutboxMessages.Add(outboxMessage);

        return Task.CompletedTask;
    }
}
