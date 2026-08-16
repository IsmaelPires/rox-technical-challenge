namespace Rox.FinancialControl.Infrastructure.Persistence;

public sealed class OutboxMessage
{
    private OutboxMessage()
    {
        Type = string.Empty;
        Payload = string.Empty;
    }

    private OutboxMessage(Guid id, string type, string payload, DateTimeOffset occurredAt)
    {
        Id = id;
        Type = type;
        Payload = payload;
        OccurredAt = occurredAt;
    }

    public Guid Id { get; private set; }

    public string Type { get; private set; }

    public string Payload { get; private set; }

    public DateTimeOffset OccurredAt { get; private set; }

    public DateTimeOffset? ProcessedAt { get; private set; }

    public int Attempts { get; private set; }

    public string? Error { get; private set; }

    public static OutboxMessage Create(string type, string payload, DateTimeOffset occurredAt)
    {
        return new OutboxMessage(Guid.NewGuid(), type, payload, occurredAt);
    }

    public void MarkProcessed(DateTimeOffset processedAt)
    {
        ProcessedAt = processedAt;
        Error = null;
    }

    public void MarkFailed(Exception exception, DateTimeOffset failedAt)
    {
        Attempts++;
        OccurredAt = failedAt;
        Error = exception.Message.Length > 500 ? exception.Message[..500] : exception.Message;
    }
}
