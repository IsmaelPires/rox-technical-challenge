using Rox.FinancialControl.Domain.Balances;
using Rox.FinancialControl.Domain.Common;
using Rox.FinancialControl.Domain.Entries;

namespace Rox.FinancialControl.Tests.Domain;

public sealed class DailyBalanceTests
{
    [Fact]
    public void Apply_ShouldConsolidateCreditsAndDebits()
    {
        var businessDate = new DateOnly(2026, 8, 14);
        var now = DateTimeOffset.UtcNow;
        var balance = DailyBalance.Create(businessDate, CashEntryOrigin.Business, now);

        balance.Apply(
            new CashEntrySnapshot(Guid.NewGuid(), businessDate, CashEntryType.Credit, CashEntryOrigin.Business, 250m),
            now);
        balance.Apply(
            new CashEntrySnapshot(Guid.NewGuid(), businessDate, CashEntryType.Debit, CashEntryOrigin.Business, 75.50m),
            now.AddSeconds(1));

        Assert.Equal(250m, balance.TotalCredits);
        Assert.Equal(75.50m, balance.TotalDebits);
        Assert.Equal(174.50m, balance.Balance);
        Assert.Equal(2, balance.EntriesCount);
    }

    [Fact]
    public void Create_ShouldRejectDefaultBusinessDate()
    {
        var act = () => DailyBalance.Create(default, CashEntryOrigin.Business, DateTimeOffset.UtcNow);

        Assert.Throws<DomainException>(act);
    }

    [Fact]
    public void Apply_ShouldRejectEntryFromAnotherBusinessDate()
    {
        var businessDate = new DateOnly(2026, 8, 14);
        var balance = DailyBalance.Create(businessDate, CashEntryOrigin.Business, DateTimeOffset.UtcNow);

        var act = () => balance.Apply(
            new CashEntrySnapshot(
                Guid.NewGuid(),
                businessDate.AddDays(1),
                CashEntryType.Credit,
                CashEntryOrigin.Business,
                10m),
            DateTimeOffset.UtcNow);

        Assert.Throws<DomainException>(act);
    }

    [Fact]
    public void Apply_ShouldRejectNonPositiveAmount()
    {
        var businessDate = new DateOnly(2026, 8, 14);
        var balance = DailyBalance.Create(businessDate, CashEntryOrigin.Business, DateTimeOffset.UtcNow);

        var act = () => balance.Apply(
            new CashEntrySnapshot(Guid.NewGuid(), businessDate, CashEntryType.Credit, CashEntryOrigin.Business, 0m),
            DateTimeOffset.UtcNow);

        Assert.Throws<DomainException>(act);
    }

    [Fact]
    public void Apply_ShouldRejectEntryFromAnotherOrigin()
    {
        var businessDate = new DateOnly(2026, 8, 14);
        var balance = DailyBalance.Create(businessDate, CashEntryOrigin.Business, DateTimeOffset.UtcNow);

        var act = () => balance.Apply(
            new CashEntrySnapshot(
                Guid.NewGuid(),
                businessDate,
                CashEntryType.Credit,
                CashEntryOrigin.Validation,
                10m),
            DateTimeOffset.UtcNow);

        Assert.Throws<DomainException>(act);
    }
}
