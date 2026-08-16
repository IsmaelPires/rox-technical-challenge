using Rox.FinancialControl.Application.Abstractions;
using Rox.FinancialControl.Application.Common;

namespace Rox.FinancialControl.Application.CashEntries;

public sealed class GetCashEntryHandler(ICashEntryRepository cashEntryRepository)
{
    public async Task<CashEntryDto> HandleAsync(Guid id, CancellationToken cancellationToken)
    {
        var entry = await cashEntryRepository.GetByIdAsync(id, cancellationToken);

        return entry?.ToDto() ?? throw new ValidationException("Lançamento não encontrado.");
    }
}
