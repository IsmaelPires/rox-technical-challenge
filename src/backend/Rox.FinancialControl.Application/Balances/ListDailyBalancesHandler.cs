using Rox.FinancialControl.Application.Abstractions;
using Rox.FinancialControl.Application.Common;
using Rox.FinancialControl.Domain.Entries;

namespace Rox.FinancialControl.Application.Balances;

public sealed class ListDailyBalancesHandler(IDailyBalanceRepository dailyBalanceRepository)
{
    public async Task<IReadOnlyCollection<DailyBalanceDto>> HandleAsync(
        DateOnly? from,
        DateOnly? to,
        string? origin,
        CancellationToken cancellationToken)
    {
        if (from.HasValue && to.HasValue && from > to)
        {
            throw new ValidationException("A data inicial não pode ser maior que a data final.");
        }

        var parsedOrigin = ParseOrigin(origin);
        var balances = await dailyBalanceRepository.ListAsync(
            new DailyBalanceQuery(from, to, parsedOrigin),
            cancellationToken);

        return balances.Select(balance => balance.ToDto()).ToArray();
    }

    private static CashEntryOrigin ParseOrigin(string? origin)
    {
        if (string.IsNullOrWhiteSpace(origin))
        {
            return CashEntryOrigin.Business;
        }

        if (Enum.TryParse<CashEntryOrigin>(origin, ignoreCase: true, out var parsedOrigin))
        {
            return parsedOrigin;
        }

        throw new ValidationException("A origem deve ser Business, Validation ou LoadSimulation.");
    }
}
