using Rox.FinancialControl.Application.Abstractions;
using Rox.FinancialControl.Application.Common;

namespace Rox.FinancialControl.Application.Balances;

public sealed class ListDailyBalancesHandler(IDailyBalanceRepository dailyBalanceRepository)
{
    public async Task<IReadOnlyCollection<DailyBalanceDto>> HandleAsync(
        DateOnly? from,
        DateOnly? to,
        CancellationToken cancellationToken)
    {
        if (from.HasValue && to.HasValue && from > to)
        {
            throw new ValidationException("A data inicial não pode ser maior que a data final.");
        }

        var balances = await dailyBalanceRepository.ListAsync(
            new DailyBalanceQuery(from, to),
            cancellationToken);

        return balances.Select(balance => balance.ToDto()).ToArray();
    }
}
