using Microsoft.EntityFrameworkCore;
using Rox.FinancialControl.Application.Abstractions;
using Rox.FinancialControl.Application.CashEntries;
using Rox.FinancialControl.Application.Common;
using Rox.FinancialControl.Domain.Entries;
using Rox.FinancialControl.Infrastructure.Persistence;

namespace Rox.FinancialControl.Infrastructure.Repositories;

public sealed class EfCashEntryRepository(ApplicationDbContext dbContext) : ICashEntryRepository
{
    public async Task AddAsync(CashEntry entry, CancellationToken cancellationToken)
    {
        await dbContext.CashEntries.AddAsync(entry, cancellationToken);
    }

    public Task<CashEntry?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return dbContext.CashEntries
            .AsNoTracking()
            .SingleOrDefaultAsync(entry => entry.Id == id, cancellationToken);
    }

    public async Task<PagedResult<CashEntry>> ListAsync(CashEntryQuery query, CancellationToken cancellationToken)
    {
        var entries = dbContext.CashEntries.AsNoTracking();

        if (query.From.HasValue)
        {
            entries = entries.Where(entry => entry.BusinessDate >= query.From.Value);
        }

        if (query.To.HasValue)
        {
            entries = entries.Where(entry => entry.BusinessDate <= query.To.Value);
        }

        if (query.Type.HasValue)
        {
            entries = entries.Where(entry => entry.Type == query.Type.Value);
        }

        entries = entries.Where(entry => entry.Origin == query.Origin);

        var totalItems = await entries.CountAsync(cancellationToken);
        var items = await entries
            .OrderByDescending(entry => entry.BusinessDate)
            .ThenByDescending(entry => entry.RegisteredAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToArrayAsync(cancellationToken);

        return new PagedResult<CashEntry>(items, query.Page, query.PageSize, totalItems);
    }
}
