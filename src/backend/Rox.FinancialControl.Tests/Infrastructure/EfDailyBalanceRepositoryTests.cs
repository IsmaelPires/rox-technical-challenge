using Microsoft.EntityFrameworkCore;
using Rox.FinancialControl.Application.Balances;
using Rox.FinancialControl.Domain.Balances;
using Rox.FinancialControl.Domain.Entries;
using Rox.FinancialControl.Infrastructure.Persistence;
using Rox.FinancialControl.Infrastructure.Repositories;

namespace Rox.FinancialControl.Tests.Infrastructure;

public sealed class EfDailyBalanceRepositoryTests
{
    [Fact]
    public async Task ListAsync_ShouldFilterByRangeAndOrderByMostRecentDate()
    {
        await using var dbContext = CreateDbContext();
        var repository = new EfDailyBalanceRepository(dbContext);

        dbContext.DailyBalances.AddRange(
            CreateBalance(new DateOnly(2026, 8, 13)),
            CreateBalance(new DateOnly(2026, 8, 14)),
            CreateBalance(new DateOnly(2026, 8, 15)));
        await dbContext.SaveChangesAsync();

        var result = await repository.ListAsync(
            new DailyBalanceQuery(
                new DateOnly(2026, 8, 14),
                new DateOnly(2026, 8, 15),
                CashEntryOrigin.Business),
            CancellationToken.None);

        Assert.Collection(
            result,
            balance => Assert.Equal(new DateOnly(2026, 8, 15), balance.BusinessDate),
            balance => Assert.Equal(new DateOnly(2026, 8, 14), balance.BusinessDate));
    }

    [Fact]
    public async Task GetByDateAsync_ShouldReturnMatchingBalance()
    {
        await using var dbContext = CreateDbContext();
        var repository = new EfDailyBalanceRepository(dbContext);
        var expectedDate = new DateOnly(2026, 8, 15);

        dbContext.DailyBalances.Add(CreateBalance(expectedDate));
        await dbContext.SaveChangesAsync();

        var result = await repository.GetByDateAsync(expectedDate, CashEntryOrigin.Business, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(expectedDate, result.BusinessDate);
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    private static DailyBalance CreateBalance(DateOnly businessDate)
    {
        return DailyBalance.Create(
            businessDate,
            CashEntryOrigin.Business,
            new DateTimeOffset(2026, 8, businessDate.Day, 12, 0, 0, TimeSpan.Zero));
    }
}
