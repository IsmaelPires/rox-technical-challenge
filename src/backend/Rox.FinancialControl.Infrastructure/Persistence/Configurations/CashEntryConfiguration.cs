using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rox.FinancialControl.Domain.Entries;

namespace Rox.FinancialControl.Infrastructure.Persistence.Configurations;

public sealed class CashEntryConfiguration : IEntityTypeConfiguration<CashEntry>
{
    public void Configure(EntityTypeBuilder<CashEntry> builder)
    {
        builder.ToTable("cash_entries");

        builder.HasKey(entry => entry.Id);

        builder.Property(entry => entry.BusinessDate)
            .HasColumnType("date")
            .IsRequired();

        builder.Property(entry => entry.Type)
            .HasConversion<string>()
            .HasMaxLength(16)
            .IsRequired();

        builder.Property(entry => entry.Origin)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(entry => entry.Amount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(entry => entry.Description)
            .HasMaxLength(180)
            .IsRequired();

        builder.Property(entry => entry.OccurredAt)
            .IsRequired();

        builder.Property(entry => entry.RegisteredAt)
            .IsRequired();

        builder.HasIndex(entry => entry.BusinessDate);
        builder.HasIndex(entry => new { entry.BusinessDate, entry.Origin });
        builder.HasIndex(entry => entry.RegisteredAt);
    }
}
