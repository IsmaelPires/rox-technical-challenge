namespace Rox.FinancialControl.Application.CashEntries;

public sealed record CashEntryDto(
    Guid Id,
    DateOnly BusinessDate,
    string Type,
    string Origin,
    decimal Amount,
    string Description,
    DateTimeOffset OccurredAt,
    DateTimeOffset RegisteredAt);
