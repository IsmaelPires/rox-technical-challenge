namespace Rox.FinancialControl.Application.CashEntries;

public sealed record CashEntryDto(
    Guid Id,
    DateOnly BusinessDate,
    string Type,
    decimal Amount,
    string Description,
    DateTimeOffset OccurredAt,
    DateTimeOffset RegisteredAt);
