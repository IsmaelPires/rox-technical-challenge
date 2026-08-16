using Rox.FinancialControl.Application.Balances;
using Rox.FinancialControl.Domain.Balances;

namespace Rox.FinancialControl.Application.Abstractions;

public interface IDailyBalanceRepository
{
    Task<DailyBalance?> GetByDateAsync(DateOnly businessDate, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<DailyBalance>> ListAsync(DailyBalanceQuery query, CancellationToken cancellationToken);
}
