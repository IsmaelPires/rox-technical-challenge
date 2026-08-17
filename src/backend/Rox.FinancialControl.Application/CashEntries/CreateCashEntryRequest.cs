namespace Rox.FinancialControl.Application.CashEntries;

public sealed record CreateCashEntryRequest(
    DateOnly BusinessDate,
    string Type,
    decimal Amount,
    string Description,
    DateTimeOffset? OccurredAt,
    string? Origin = null);
