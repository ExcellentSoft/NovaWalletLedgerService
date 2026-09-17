using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NovaWallet.Domain.Entities;

namespace NovaWallet.Infrastructure.Persistence.Configurations;

public class IdempotencyRecordConfiguration : IEntityTypeConfiguration<IdempotencyRecord>
{
    public void Configure(EntityTypeBuilder<IdempotencyRecord> builder)
    {
        builder.ToTable("IdempotencyRecords");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.Id)
            .ValueGeneratedNever();

        builder.Property(i => i.Key)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(i => i.RequestHash)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(i => i.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(i => i.ResponsePayload)
            .HasColumnType("jsonb");

        builder.Property(i => i.CreatedAtUtc)
            .IsRequired();

        builder.HasIndex(i => i.Key)
            .IsUnique();
    }
}
