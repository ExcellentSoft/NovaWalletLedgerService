using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NovaWallet.Domain.Entities;

namespace NovaWallet.Infrastructure.Persistence.Configurations;

public class WalletConfiguration : IEntityTypeConfiguration<Wallet>
{
    public void Configure(EntityTypeBuilder<Wallet> builder)
    {
        builder.ToTable("Wallets");

        builder.HasKey(w => w.Id);

        builder.Property(w => w.Id)
            .ValueGeneratedNever();

        builder.Property(w => w.OwnerId)
            .IsRequired();

        builder.Property(w => w.Balance)
            .IsRequired();

        builder.Property(w => w.Currency)
            .IsRequired()
            .HasMaxLength(3);

        builder.Property(w => w.CreatedAtUtc)
            .IsRequired();

        builder.Property(w => w.IsActive)
            .IsRequired();

        builder.HasIndex(w => w.OwnerId);
    }
}
