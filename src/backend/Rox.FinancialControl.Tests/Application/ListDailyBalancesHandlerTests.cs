using Rox.FinancialControl.Application.Abstractions;
using Rox.FinancialControl.Application.Balances;
using Rox.FinancialControl.Application.Common;
using Rox.FinancialControl.Domain.Balances;
using Rox.FinancialControl.Domain.Entries;

namespace Rox.FinancialControl.Tests.Application;

public sealed class ListDailyBalancesHandlerTests
{
    [Fact]
    public async Task HandleAsync_ShouldRejectInvalidDateRange()
    {
        var handler = new ListDailyBalancesHandler(new CapturingDailyBalanceRepository([]));

        await Assert.ThrowsAsync<ValidationException>(() =>
            handler.HandleAsync(
                new DateOnly(2026, 8, 15),
                new DateOnly(2026, 8, 10),
                null,
                CancellationToken.None));
    }

    [Fact]
    public async Task HandleAsync_ShouldMapRepositoryResults()
    {
        var balance = DailyBalance.Create(
            new DateOnly(2026, 8, 15),
            CashEntryOrigin.Business,
            new DateTimeOffset(2026, 8, 15, 12, 0, 0, TimeSpan.Zero));
        var handler = new ListDailyBalancesHandler(new CapturingDailyBalanceRepository([balance]));

        var result = await handler.HandleAsync(
            new DateOnly(2026, 8, 15),
            new DateOnly(2026, 8, 15),
            "Business",
            CancellationToken.None);

        var dto = Assert.Single(result);
        Assert.Equal(balance.BusinessDate, dto.BusinessDate);
        Assert.Equal(balance.Balance, dto.Balance);
    }

    private sealed class CapturingDailyBalanceRepository(IReadOnlyCollection<DailyBalance> balances)
        : IDailyBalanceRepository
    {
        public DailyBalanceQuery? LastQuery { get; private set; }

        public Task<DailyBalance?> GetByDateAsync(
            DateOnly businessDate,
            CashEntryOrigin origin,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(balances.SingleOrDefault(balance =>
                balance.BusinessDate == businessDate && balance.Origin == origin));
        }

        public Task<IReadOnlyCollection<DailyBalance>> ListAsync(
            DailyBalanceQuery query,
            CancellationToken cancellationToken)
        {
            LastQuery = query;
            return Task.FromResult(balances);
        }
    }
}
