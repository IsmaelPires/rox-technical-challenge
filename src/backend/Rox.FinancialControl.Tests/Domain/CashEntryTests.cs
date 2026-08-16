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
            10.235m,
            "  Venda balcao  ",
            now,
            now);

        Assert.Equal(10.24m, entry.Amount);
        Assert.Equal("Venda balcao", entry.Description);
    }
}
