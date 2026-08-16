using Rox.FinancialControl.Application.Abstractions;
using Rox.FinancialControl.Application.Common;
using Rox.FinancialControl.Application.Messaging;
using Rox.FinancialControl.Domain.Entries;

namespace Rox.FinancialControl.Application.CashEntries;

public sealed class CreateCashEntryHandler(
    ICashEntryRepository cashEntryRepository,
    IOutboxWriter outboxWriter,
    IUnitOfWork unitOfWork,
    IClock clock)
{
    public async Task<CashEntryDto> HandleAsync(CreateCashEntryRequest request, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<CashEntryType>(request.Type, ignoreCase: true, out var type))
        {
            throw new ValidationException("O tipo deve ser Credit ou Debit.");
        }

        var registeredAt = clock.UtcNow;
        var occurredAt = request.OccurredAt ?? registeredAt;
        var entry = CashEntry.Create(
            request.BusinessDate,
            type,
            request.Amount,
            request.Description,
            occurredAt,
            registeredAt);

        await cashEntryRepository.AddAsync(entry, cancellationToken);
        await outboxWriter.AddAsync(
            new CashEntryRegisteredIntegrationEvent(
                entry.Id,
                entry.BusinessDate,
                entry.Type.ToString(),
                entry.Amount,
                entry.Description,
                entry.OccurredAt,
                entry.RegisteredAt),
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return entry.ToDto();
    }
}
