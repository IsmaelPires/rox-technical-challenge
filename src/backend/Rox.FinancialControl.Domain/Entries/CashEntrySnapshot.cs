namespace Rox.FinancialControl.Domain.Entries;

public sealed record CashEntrySnapshot(
    Guid CashEntryId,
    DateOnly BusinessDate,
    CashEntryType Type,
    CashEntryOrigin Origin,
    decimal Amount);
