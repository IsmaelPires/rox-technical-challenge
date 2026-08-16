using Rox.FinancialControl.Application.Abstractions;
using Rox.FinancialControl.Infrastructure.Persistence;

namespace Rox.FinancialControl.Infrastructure.Repositories;

public sealed class EfUnitOfWork(ApplicationDbContext dbContext) : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
