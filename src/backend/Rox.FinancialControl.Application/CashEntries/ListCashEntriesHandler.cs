using Rox.FinancialControl.Application.Abstractions;
using Rox.FinancialControl.Application.Common;
using Rox.FinancialControl.Domain.Entries;

namespace Rox.FinancialControl.Application.CashEntries;

public sealed class ListCashEntriesHandler(ICashEntryRepository cashEntryRepository)
{
    public async Task<PagedResult<CashEntryDto>> HandleAsync(
        DateOnly? from,
        DateOnly? to,
        string? type,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        if (from.HasValue && to.HasValue && from > to)
        {
            throw new ValidationException("A data inicial não pode ser maior que a data final.");
        }

        CashEntryType? parsedType = null;
        if (!string.IsNullOrWhiteSpace(type))
        {
            if (!Enum.TryParse<CashEntryType>(type, ignoreCase: true, out var entryType))
            {
                throw new ValidationException("O tipo deve ser Credit ou Debit.");
            }

            parsedType = entryType;
        }

        var normalizedPage = Math.Max(1, page);
        var normalizedPageSize = Math.Clamp(pageSize, 1, 100);
        var result = await cashEntryRepository.ListAsync(
            new CashEntryQuery(from, to, parsedType, normalizedPage, normalizedPageSize),
            cancellationToken);

        return new PagedResult<CashEntryDto>(
            result.Items.Select(entry => entry.ToDto()).ToArray(),
            result.Page,
            result.PageSize,
            result.TotalItems);
    }
}
