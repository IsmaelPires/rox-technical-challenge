using Rox.FinancialControl.Domain.Balances;

namespace Rox.FinancialControl.Application.Balances;

public static class DailyBalanceMappings
{
    public static DailyBalanceDto ToDto(this DailyBalance balance)
    {
        return new DailyBalanceDto(
            balance.BusinessDate,
            balance.TotalCredits,
            balance.TotalDebits,
            balance.Balance,
            balance.EntriesCount,
            balance.LastUpdatedAt);
    }
}
