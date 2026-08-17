using Microsoft.EntityFrameworkCore;
using Rox.FinancialControl.Application.CashEntries;
using Rox.FinancialControl.Domain.Entries;
using Rox.FinancialControl.Infrastructure.Persistence;
using Rox.FinancialControl.Infrastructure.Repositories;

namespace Rox.FinancialControl.Tests.Infrastructure;

public sealed class EfCashEntryRepositoryTests
{
    [Fact]
    public async Task ListAsync_ShouldFilterByDateAndTypeAndReturnPagedResult()
    {
        await using var dbContext = CreateDbContext();
        var repository = new EfCashEntryRepository(dbContext);

        dbContext.CashEntries.AddRange(
            CreateEntry(new DateOnly(2026, 8, 14), CashEntryType.Credit, 100m, 1),
            CreateEntry(new DateOnly(2026, 8, 15), CashEntryType.Debit, 40m, 2),
            CreateEntry(new DateOnly(2026, 8, 15), CashEntryType.Debit, 55m, 3),
            CreateEntry(new DateOnly(2026, 8, 16), CashEntryType.Credit, 90m, 4));
        await dbContext.SaveChangesAsync();

        var result = await repository.ListAsync(
            new CashEntryQuery(
                new DateOnly(2026, 8, 15),
                new DateOnly(2026, 8, 15),
                CashEntryType.Debit,
                CashEntryOrigin.Business,
                Page: 1,
                PageSize: 1),
            CancellationToken.None);

        Assert.Equal(2, result.TotalItems);
        Assert.Equal(2, result.TotalPages);
        Assert.Single(result.Items);
        Assert.Equal(55m, result.Items.Single().Amount);
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    private static CashEntry CreateEntry(DateOnly businessDate, CashEntryType type, decimal amount, int minutes)
    {
        var registeredAt = new DateTimeOffset(2026, 8, businessDate.Day, 12, minutes, 0, TimeSpan.Zero);

        return CashEntry.Create(
            businessDate,
            type,
            CashEntryOrigin.Business,
            amount,
            $"Lançamento {minutes}",
            registeredAt,
            registeredAt);
    }
}
