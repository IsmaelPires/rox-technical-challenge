using Rox.FinancialControl.Domain.Entries;

namespace Rox.FinancialControl.Application.CashEntries;

public sealed record CashEntryQuery(
    DateOnly? From,
    DateOnly? To,
    CashEntryType? Type,
    CashEntryOrigin Origin,
    int Page,
    int PageSize);
