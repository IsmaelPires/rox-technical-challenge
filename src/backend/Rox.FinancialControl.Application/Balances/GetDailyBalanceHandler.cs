using Rox.FinancialControl.Application.Abstractions;
using Rox.FinancialControl.Application.Common;

namespace Rox.FinancialControl.Application.Balances;

public sealed class GetDailyBalanceHandler(IDailyBalanceRepository dailyBalanceRepository)
{
    public async Task<DailyBalanceDto> HandleAsync(DateOnly businessDate, CancellationToken cancellationToken)
    {
        var balance = await dailyBalanceRepository.GetByDateAsync(businessDate, cancellationToken);

        return balance?.ToDto() ?? throw new ValidationException("Saldo diário ainda não consolidado.");
    }
}
