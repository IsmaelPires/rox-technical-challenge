using Microsoft.EntityFrameworkCore;
using Rox.FinancialControl.Application.Abstractions;
using Rox.FinancialControl.Application.Balances;
using Rox.FinancialControl.Domain.Balances;
using Rox.FinancialControl.Infrastructure.Persistence;

namespace Rox.FinancialControl.Infrastructure.Repositories;

public sealed class EfDailyBalanceRepository(ApplicationDbContext dbContext) : IDailyBalanceRepository
{
    public Task<DailyBalance?> GetByDateAsync(DateOnly businessDate, CancellationToken cancellationToken)
    {
        return dbContext.DailyBalances
            .AsNoTracking()
            .SingleOrDefaultAsync(balance => balance.BusinessDate == businessDate, cancellationToken);
    }

    public async Task<IReadOnlyCollection<DailyBalance>> ListAsync(
        DailyBalanceQuery query,
        CancellationToken cancellationToken)
    {
        var balances = dbContext.DailyBalances.AsNoTracking();

        if (query.From.HasValue)
        {
            balances = balances.Where(balance => balance.BusinessDate >= query.From.Value);
        }

        if (query.To.HasValue)
        {
            balances = balances.Where(balance => balance.BusinessDate <= query.To.Value);
        }

        return await balances
            .OrderByDescending(balance => balance.BusinessDate)
            .ToArrayAsync(cancellationToken);
    }
}
