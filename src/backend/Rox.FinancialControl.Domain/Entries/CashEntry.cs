using Rox.FinancialControl.Domain.Common;

namespace Rox.FinancialControl.Domain.Entries;

public sealed class CashEntry
{
    private CashEntry()
    {
        Description = string.Empty;
    }

    private CashEntry(
        Guid id,
        DateOnly businessDate,
        CashEntryType type,
        decimal amount,
        string description,
        DateTimeOffset occurredAt,
        DateTimeOffset registeredAt)
    {
        Id = id;
        BusinessDate = businessDate;
        Type = type;
        Amount = amount;
        Description = description;
        OccurredAt = occurredAt;
        RegisteredAt = registeredAt;
    }

    public Guid Id { get; private set; }

    public DateOnly BusinessDate { get; private set; }

    public CashEntryType Type { get; private set; }

    public decimal Amount { get; private set; }

    public string Description { get; private set; }

    public DateTimeOffset OccurredAt { get; private set; }

    public DateTimeOffset RegisteredAt { get; private set; }

    public static CashEntry Create(
        DateOnly businessDate,
        CashEntryType type,
        decimal amount,
        string description,
        DateTimeOffset occurredAt,
        DateTimeOffset registeredAt)
    {
        if (businessDate == default)
        {
            throw new DomainException("A data de negócio do lançamento é obrigatória.");
        }

        if (!Enum.IsDefined(type))
        {
            throw new DomainException("O tipo do lançamento deve ser Credit ou Debit.");
        }

        if (amount <= 0)
        {
            throw new DomainException("O valor do lançamento deve ser maior que zero.");
        }

        var normalizedDescription = description?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(normalizedDescription))
        {
            throw new DomainException("A descrição do lançamento é obrigatória.");
        }

        if (normalizedDescription.Length > 180)
        {
            throw new DomainException("A descrição do lançamento deve ter no máximo 180 caracteres.");
        }

        if (occurredAt > registeredAt.AddMinutes(5))
        {
            throw new DomainException("A data de ocorrência não pode estar no futuro.");
        }

        return new CashEntry(
            Guid.NewGuid(),
            businessDate,
            type,
            decimal.Round(amount, 2, MidpointRounding.AwayFromZero),
            normalizedDescription,
            occurredAt,
            registeredAt);
    }

    public CashEntrySnapshot ToSnapshot()
    {
        return new CashEntrySnapshot(Id, BusinessDate, Type, Amount);
    }
}
