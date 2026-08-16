using Rox.FinancialControl.Domain.Common;
using Rox.FinancialControl.Domain.Entries;

namespace Rox.FinancialControl.Domain.Balances;

public sealed class DailyBalance
{
    private DailyBalance()
    {
    }

    private DailyBalance(DateOnly businessDate, DateTimeOffset createdAt)
    {
        BusinessDate = businessDate;
        LastUpdatedAt = createdAt;
    }

    public DateOnly BusinessDate { get; private set; }

    public decimal TotalCredits { get; private set; }

    public decimal TotalDebits { get; private set; }

    public decimal Balance => TotalCredits - TotalDebits;

    public int EntriesCount { get; private set; }

    public DateTimeOffset LastUpdatedAt { get; private set; }

    public static DailyBalance Create(DateOnly businessDate, DateTimeOffset createdAt)
    {
        if (businessDate == default)
        {
            throw new DomainException("A data do saldo diário é obrigatória.");
        }

        return new DailyBalance(businessDate, createdAt);
    }

    public void Apply(CashEntrySnapshot entry, DateTimeOffset processedAt)
    {
        if (entry.BusinessDate != BusinessDate)
        {
            throw new DomainException("O lançamento não pertence ao dia consolidado.");
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
