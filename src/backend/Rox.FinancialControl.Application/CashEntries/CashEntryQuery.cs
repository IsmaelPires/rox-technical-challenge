using Rox.FinancialControl.Domain.Entries;

namespace Rox.FinancialControl.Application.CashEntries;

public sealed record CashEntryQuery(
    DateOnly? From,
    DateOnly? To,
    CashEntryType? Type,
    int Page,
    int PageSize);
