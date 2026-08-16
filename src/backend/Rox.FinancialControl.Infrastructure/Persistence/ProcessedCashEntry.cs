namespace Rox.FinancialControl.Infrastructure.Persistence;

public sealed class ProcessedCashEntry
{
    private ProcessedCashEntry()
    {
    }

    public ProcessedCashEntry(Guid cashEntryId, DateTimeOffset processedAt)
    {
        CashEntryId = cashEntryId;
        ProcessedAt = processedAt;
    }

    public Guid CashEntryId { get; private set; }

    public DateTimeOffset ProcessedAt { get; private set; }
}
