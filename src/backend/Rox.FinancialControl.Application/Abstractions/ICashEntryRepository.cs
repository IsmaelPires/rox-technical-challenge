using Rox.FinancialControl.Application.CashEntries;
using Rox.FinancialControl.Application.Common;
using Rox.FinancialControl.Domain.Entries;

namespace Rox.FinancialControl.Application.Abstractions;

public interface ICashEntryRepository
{
    Task AddAsync(CashEntry entry, CancellationToken cancellationToken);

    Task<CashEntry?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<PagedResult<CashEntry>> ListAsync(CashEntryQuery query, CancellationToken cancellationToken);
}
