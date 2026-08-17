using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rox.FinancialControl.Domain.Balances;

namespace Rox.FinancialControl.Infrastructure.Persistence.Configurations;

public sealed class DailyBalanceConfiguration : IEntityTypeConfiguration<DailyBalance>
{
    public void Configure(EntityTypeBuilder<DailyBalance> builder)
    {
        builder.ToTable("daily_balances");

        builder.HasKey(balance => new { balance.BusinessDate, balance.Origin });

        builder.Property(balance => balance.BusinessDate)
            .HasColumnType("date");

        builder.Property(balance => balance.Origin)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(balance => balance.TotalCredits)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(balance => balance.TotalDebits)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Ignore(balance => balance.Balance);

        builder.Property(balance => balance.EntriesCount)
            .IsRequired();

        builder.Property(balance => balance.LastUpdatedAt)
            .IsRequired();
    }
}
