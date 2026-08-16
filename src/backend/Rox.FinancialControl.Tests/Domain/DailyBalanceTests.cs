using Rox.FinancialControl.Domain.Balances;
using Rox.FinancialControl.Domain.Entries;

namespace Rox.FinancialControl.Tests.Domain;

public sealed class DailyBalanceTests
{
    [Fact]
    public void Apply_ShouldConsolidateCreditsAndDebits()
    {
        var businessDate = new DateOnly(2026, 8, 14);
        var now = DateTimeOffset.UtcNow;
        var balance = DailyBalance.Create(businessDate, now);

        balance.Apply(
            new CashEntrySnapshot(Guid.NewGuid(), businessDate, CashEntryType.Credit, 250m),
            now);
        balance.Apply(
            new CashEntrySnapshot(Guid.NewGuid(), businessDate, CashEntryType.Debit, 75.50m),
            now.AddSeconds(1));

        Assert.Equal(250m, balance.TotalCredits);
        Assert.Equal(75.50m, balance.TotalDebits);
        Assert.Equal(174.50m, balance.Balance);
        Assert.Equal(2, balance.EntriesCount);
    }
}
