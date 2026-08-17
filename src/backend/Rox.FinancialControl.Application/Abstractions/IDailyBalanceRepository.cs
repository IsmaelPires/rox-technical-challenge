using Rox.FinancialControl.Application.Balances;
using Rox.FinancialControl.Domain.Balances;
using Rox.FinancialControl.Domain.Entries;

namespace Rox.FinancialControl.Application.Abstractions;

public interface IDailyBalanceRepository
{
    Task<DailyBalance?> GetByDateAsync(
        DateOnly businessDate,
        CashEntryOrigin origin,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<DailyBalance>> ListAsync(DailyBalanceQuery query, CancellationToken cancellationToken);
}
