using Microsoft.EntityFrameworkCore;
using Rox.FinancialControl.Domain.Balances;
using Rox.FinancialControl.Domain.Entries;

namespace Rox.FinancialControl.Infrastructure.Persistence;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<CashEntry> CashEntries => Set<CashEntry>();

    public DbSet<DailyBalance> DailyBalances => Set<DailyBalance>();

    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    public DbSet<ProcessedCashEntry> ProcessedCashEntries => Set<ProcessedCashEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
