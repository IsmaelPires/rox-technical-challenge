using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Rox.FinancialControl.Infrastructure.Persistence.Configurations;

public sealed class ProcessedCashEntryConfiguration : IEntityTypeConfiguration<ProcessedCashEntry>
{
    public void Configure(EntityTypeBuilder<ProcessedCashEntry> builder)
    {
        builder.ToTable("processed_cash_entries");

        builder.HasKey(entry => entry.CashEntryId);

        builder.Property(entry => entry.ProcessedAt)
            .IsRequired();
    }
}
