using Rox.FinancialControl.Domain.Common;
using Rox.FinancialControl.Domain.Entries;

namespace Rox.FinancialControl.Domain.Balances;

public sealed class DailyBalance
{
    private DailyBalance()
    {
    }

    private DailyBalance(DateOnly businessDate, CashEntryOrigin origin, DateTimeOffset createdAt)
    {
        BusinessDate = businessDate;
        Origin = origin;
        LastUpdatedAt = createdAt;
    }

    public DateOnly BusinessDate { get; private set; }

    public CashEntryOrigin Origin { get; private set; }

    public decimal TotalCredits { get; private set; }

    public decimal TotalDebits { get; private set; }

    public decimal Balance => TotalCredits - TotalDebits;

    public int EntriesCount { get; private set; }

    public DateTimeOffset LastUpdatedAt { get; private set; }

    public static DailyBalance Create(DateOnly businessDate, CashEntryOrigin origin, DateTimeOffset createdAt)
    {
        if (businessDate == default)
        {
            throw new DomainException("A data do saldo diário é obrigatória.");
        }

        if (!Enum.IsDefined(origin))
        {
            throw new DomainException("A origem do saldo diário é inválida.");
        }

        return new DailyBalance(businessDate, origin, createdAt);
    }

    public void Apply(CashEntrySnapshot entry, DateTimeOffset processedAt)
    {
        if (entry.BusinessDate != BusinessDate)
        {
            throw new DomainException("O lançamento não pertence ao dia consolidado.");
        }

        if (entry.Origin != Origin)
        {
            throw new DomainException("O lançamento não pertence à origem consolidada.");
        }

        if (entry.Amount <= 0)
        {
            throw new DomainException("O valor consolidado deve ser maior que zero.");
        }

        if (entry.Type == CashEntryType.Credit)
        {
            TotalCredits += entry.Amount;
        }
        else if (entry.Type == CashEntryType.Debit)
        {
            TotalDebits += entry.Amount;
        }
        else
        {
            throw new DomainException("Tipo de lançamento inválido para consolidação.");
        }

        EntriesCount++;
        LastUpdatedAt = processedAt;
    }
}
