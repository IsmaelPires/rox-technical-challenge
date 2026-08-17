using Rox.FinancialControl.Application.Abstractions;
using Rox.FinancialControl.Application.Common;
using Rox.FinancialControl.Domain.Entries;

namespace Rox.FinancialControl.Application.Balances;

public sealed class GetDailyBalanceHandler(IDailyBalanceRepository dailyBalanceRepository)
{
    public async Task<DailyBalanceDto> HandleAsync(
        DateOnly businessDate,
        string? origin,
        CancellationToken cancellationToken)
    {
        var parsedOrigin = ParseOrigin(origin);
        var balance = await dailyBalanceRepository.GetByDateAsync(businessDate, parsedOrigin, cancellationToken);

        return balance?.ToDto() ?? throw new ValidationException("Saldo diário ainda não consolidado.");
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
