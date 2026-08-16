using Rox.FinancialControl.Application.Abstractions;
using Rox.FinancialControl.Application.CashEntries;
using Rox.FinancialControl.Application.Common;
using Rox.FinancialControl.Application.Messaging;
using Rox.FinancialControl.Domain.Entries;

namespace Rox.FinancialControl.Tests.Application;

public sealed class CreateCashEntryHandlerTests
{
    [Fact]
    public async Task HandleAsync_ShouldPersistEntryAndOutboxMessage()
    {
        var repository = new FakeCashEntryRepository();
        var outbox = new FakeOutboxWriter();
        var unitOfWork = new FakeUnitOfWork();
        var clock = new FixedClock(new DateTimeOffset(2026, 8, 14, 12, 0, 0, TimeSpan.Zero));
        var handler = new CreateCashEntryHandler(repository, outbox, unitOfWork, clock);

        var response = await handler.HandleAsync(
            new CreateCashEntryRequest(
                new DateOnly(2026, 8, 14),
                "Credit",
                199.90m,
                "Venda no cartao",
                null),
            CancellationToken.None);

        Assert.NotEqual(Guid.Empty, response.Id);
        Assert.Single(repository.Entries);
        Assert.Single(outbox.Messages);
        Assert.Equal(1, unitOfWork.SaveChangesCalls);

        var message = Assert.IsType<CashEntryRegisteredIntegrationEvent>(outbox.Messages.Single());
        Assert.Equal(response.Id, message.CashEntryId);
        Assert.Equal("Credit", message.Type);
        Assert.Equal(199.90m, message.Amount);
    }

    [Fact]
    public async Task HandleAsync_ShouldRejectInvalidType()
    {
        var handler = new CreateCashEntryHandler(
            new FakeCashEntryRepository(),
            new FakeOutboxWriter(),
            new FakeUnitOfWork(),
            new FixedClock(DateTimeOffset.UtcNow));

        await Assert.ThrowsAsync<ValidationException>(() =>
            handler.HandleAsync(
                new CreateCashEntryRequest(
                    new DateOnly(2026, 8, 14),
                    "Entrada",
                    10m,
                    "Venda",
                    null),
                CancellationToken.None));
    }

    private sealed class FakeCashEntryRepository : ICashEntryRepository
    {
        public List<CashEntry> Entries { get; } = [];

        public Task AddAsync(CashEntry entry, CancellationToken cancellationToken)
        {
            Entries.Add(entry);
            return Task.CompletedTask;
        }

        public Task<CashEntry?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            return Task.FromResult(Entries.SingleOrDefault(entry => entry.Id == id));
        }

        public Task<PagedResult<CashEntry>> ListAsync(CashEntryQuery query, CancellationToken cancellationToken)
        {
            return Task.FromResult(new PagedResult<CashEntry>(Entries, query.Page, query.PageSize, Entries.Count));
        }
    }

    private sealed class FakeOutboxWriter : IOutboxWriter
    {
        public List<object> Messages { get; } = [];

        public Task AddAsync<TMessage>(TMessage message, CancellationToken cancellationToken)
            where TMessage : notnull
        {
            Messages.Add(message);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public int SaveChangesCalls { get; private set; }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveChangesCalls++;
            return Task.FromResult(1);
        }
    }

    private sealed class FixedClock(DateTimeOffset utcNow) : IClock
    {
        public DateTimeOffset UtcNow { get; } = utcNow;
    }
}
