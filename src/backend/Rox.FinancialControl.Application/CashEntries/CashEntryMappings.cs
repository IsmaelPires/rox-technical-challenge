using Rox.FinancialControl.Domain.Entries;

namespace Rox.FinancialControl.Application.CashEntries;

public static class CashEntryMappings
{
    public static CashEntryDto ToDto(this CashEntry entry)
    {
        return new CashEntryDto(
            entry.Id,
            entry.BusinessDate,
            entry.Type.ToString(),
            entry.Origin.ToString(),
            entry.Amount,
            entry.Description,
            entry.OccurredAt,
            entry.RegisteredAt);
    }
}
