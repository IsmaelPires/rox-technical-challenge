using Rox.FinancialControl.Application.Abstractions;
using Rox.FinancialControl.Application.CashEntries;
using Rox.FinancialControl.Application.Common;
using Rox.FinancialControl.Domain.Entries;

namespace Rox.FinancialControl.Tests.Application;

public sealed class ListCashEntriesHandlerTests
{
    [Fact]
    public async Task HandleAsync_ShouldNormalizePaginationAndParseType()
    {
        var repository = new CapturingCashEntryRepository([
            CreateEntry(new DateOnly(2026, 8, 15), CashEntryType.Debit, 75m)
        ]);
        var handler = new ListCashEntriesHandler(repository);

        var result = await handler.HandleAsync(
            new DateOnly(2026, 8, 10),
            new DateOnly(2026, 8, 15),
            "debit",
            "Business",
            page: 0,
            pageSize: 150,
            CancellationToken.None);

        Assert.Equal(1, repository.LastQuery?.Page);
        Assert.Equal(100, repository.LastQuery?.PageSize);
        Assert.Equal(CashEntryType.Debit, repository.LastQuery?.Type);
        Assert.Equal(CashEntryOrigin.Business, repository.LastQuery?.Origin);
        Assert.Single(result.Items);
        Assert.Equal("Debit", result.Items.Single().Type);
    }

    [Fact]
    public async Task HandleAsync_ShouldRejectInvalidDateRange()
    {
        var handler = new ListCashEntriesHandler(new CapturingCashEntryRepository([]));

        await Assert.ThrowsAsync<ValidationException>(() =>
            handler.HandleAsync(
                new DateOnly(2026, 8, 15),
                new DateOnly(2026, 8, 10),
                null,
                null,
                page: 1,
                pageSize: 20,
                CancellationToken.None));
    }

    [Fact]
    public async Task HandleAsync_ShouldRejectInvalidType()
    {
        var handler = new ListCashEntriesHandler(new CapturingCashEntryRepository([]));

        await Assert.ThrowsAsync<ValidationException>(() =>
            handler.HandleAsync(
                null,
                null,
                "Entrada",
                null,
                page: 1,
                pageSize: 20,
                CancellationToken.None));
    }

    private static CashEntry CreateEntry(DateOnly businessDate, CashEntryType type, decimal amount)
    {
        var now = new DateTimeOffset(2026, 8, 15, 12, 0, 0, TimeSpan.Zero);

        return CashEntry.Create(
            businessDate,
            type,
            CashEntryOrigin.Business,
            amount,
            "Lançamento de teste",
            now,
            now);
    }

    private sealed class CapturingCashEntryRepository(IReadOnlyCollection<CashEntry> entries) : ICashEntryRepository
    {
        public CashEntryQuery? LastQuery { get; private set; }

        public Task AddAsync(CashEntry entry, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<CashEntry?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<PagedResult<CashEntry>> ListAsync(CashEntryQuery query, CancellationToken cancellationToken)
        {
            LastQuery = query;
            return Task.FromResult(new PagedResult<CashEntry>(entries, query.Page, query.PageSize, entries.Count));
        }
    }
}
