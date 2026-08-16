namespace Rox.FinancialControl.Infrastructure.Messaging;

public sealed class OutboxOptions
{
    public const string SectionName = "Outbox";

    public int BatchSize { get; init; } = 50;

    public int PollingIntervalSeconds { get; init; } = 5;
}
