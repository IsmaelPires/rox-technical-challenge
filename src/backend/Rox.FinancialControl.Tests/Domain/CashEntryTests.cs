using Rox.FinancialControl.Domain.Common;
using Rox.FinancialControl.Domain.Entries;

namespace Rox.FinancialControl.Tests.Domain;

public sealed class CashEntryTests
{
    [Fact]
    public void Create_ShouldRejectNonPositiveAmount()
    {
        var now = DateTimeOffset.UtcNow;

        var act = () => CashEntry.Create(
            DateOnly.FromDateTime(now.Date),
            CashEntryType.Credit,
            CashEntryOrigin.Business,
            0,
            "Venda balcao",
            now,
            now);

        Assert.Throws<DomainException>(act);
    }

    [Fact]
    public void Create_ShouldNormalizeAmountAndDescription()
    {
        var now = DateTimeOffset.UtcNow;

        var entry = CashEntry.Create(
            DateOnly.FromDateTime(now.Date),
            CashEntryType.Credit,
            CashEntryOrigin.Business,
            10.235m,
            "  Venda balcao  ",
            now,
            now);

        Assert.Equal(10.24m, entry.Amount);
        Assert.Equal("Venda balcao", entry.Description);
        Assert.Equal(CashEntryOrigin.Business, entry.Origin);
    }

    [Fact]
    public void Create_ShouldRejectBlankDescription()
    {
        var now = DateTimeOffset.UtcNow;

        var act = () => CashEntry.Create(
            DateOnly.FromDateTime(now.Date),
            CashEntryType.Debit,
            CashEntryOrigin.Business,
            10m,
            "   ",
            now,
            now);

        Assert.Throws<DomainException>(act);
    }

    [Fact]
    public void Create_ShouldRejectFutureOccurrence()
    {
        var now = DateTimeOffset.UtcNow;

        var act = () => CashEntry.Create(
            DateOnly.FromDateTime(now.Date),
            CashEntryType.Credit,
            CashEntryOrigin.Business,
            10m,
            "Venda balcao",
            now.AddMinutes(6),
            now);

        Assert.Throws<DomainException>(act);
    }

    [Fact]
    public void ToSnapshot_ShouldExposeConsolidationData()
    {
        var now = DateTimeOffset.UtcNow;

        var entry = CashEntry.Create(
            DateOnly.FromDateTime(now.Date),
            CashEntryType.Debit,
            CashEntryOrigin.Validation,
            42.10m,
            "Pagamento fornecedor",
            now,
            now);

        var snapshot = entry.ToSnapshot();

        Assert.Equal(entry.Id, snapshot.CashEntryId);
        Assert.Equal(entry.BusinessDate, snapshot.BusinessDate);
        Assert.Equal(entry.Type, snapshot.Type);
        Assert.Equal(entry.Origin, snapshot.Origin);
        Assert.Equal(entry.Amount, snapshot.Amount);
    }
}
