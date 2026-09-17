using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NovaWallet.Domain.Entities;

namespace NovaWallet.Infrastructure.Persistence.Configurations;

public class DailyTransferUsageConfiguration : IEntityTypeConfiguration<DailyTransferUsage>
{
    public void Configure(EntityTypeBuilder<DailyTransferUsage> builder)
    {
        builder.ToTable("DailyTransferUsages");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.Id)
            .ValueGeneratedNever();

        builder.Property(d => d.WalletId)
            .IsRequired();

        builder.Property(d => d.UsageDate)
            .IsRequired()
            .HasColumnType("date");

        builder.Property(d => d.TotalTransferred)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.HasIndex(d => new { d.WalletId, d.UsageDate })
            .IsUnique();
    }
}
