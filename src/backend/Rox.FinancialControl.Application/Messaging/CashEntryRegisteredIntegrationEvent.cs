namespace Rox.FinancialControl.Application.Messaging;

public sealed record CashEntryRegisteredIntegrationEvent(
    Guid CashEntryId,
    DateOnly BusinessDate,
    string Type,
    decimal Amount,
    string Description,
    DateTimeOffset OccurredAt,
    DateTimeOffset RegisteredAt);
