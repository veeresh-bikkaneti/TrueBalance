using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data;

public sealed class ImportBatchConfiguration : IEntityTypeConfiguration<ImportBatch>
{
    public void Configure(EntityTypeBuilder<ImportBatch> entity)
    {
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Extension).IsRequired().HasMaxLength(10);
        entity.Property(e => e.FailureReason).HasMaxLength(500);
        // Two imports of one batch cannot both stage it.
        entity.Property(e => e.Status).IsConcurrencyToken();
    }
}

public sealed class StagedRowConfiguration : IEntityTypeConfiguration<StagedRow>
{
    public void Configure(EntityTypeBuilder<StagedRow> entity)
    {
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Description).IsRequired().HasMaxLength(StagedRow.MaxDescription);
        entity.Property(e => e.MatchKey).IsRequired().HasMaxLength(StagedRow.MaxDescription);
        entity.Property(e => e.Amount).HasColumnType("numeric(18,2)");
        entity.Property(e => e.Balance).HasColumnType("numeric(18,2)");
        // A row can be posted once: a stale second post fails on save.
        entity.Property(e => e.Status).IsConcurrencyToken();
        entity.HasOne<ImportBatch>().WithMany().HasForeignKey(e => e.BatchId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Account>().WithMany().HasForeignKey(e => e.BankAccountId).OnDelete(DeleteBehavior.Restrict);
        entity.HasIndex(e => new { e.BankAccountId, e.Date });
        entity.HasIndex(e => e.MatchKey);
    }
}

public sealed class CsvMappingProfileConfiguration : IEntityTypeConfiguration<CsvMappingProfile>
{
    public void Configure(EntityTypeBuilder<CsvMappingProfile> entity)
    {
        entity.HasKey(e => e.Id);
        entity.Property(e => e.BankName).IsRequired().HasMaxLength(100);
        entity.HasIndex(e => e.BankName).IsUnique();
        entity.Property(e => e.DateColumn).IsRequired().HasMaxLength(100);
        entity.Property(e => e.DescriptionColumn).IsRequired().HasMaxLength(100);
        entity.Property(e => e.AmountColumn).HasMaxLength(100);
        entity.Property(e => e.DebitColumn).HasMaxLength(100);
        entity.Property(e => e.CreditColumn).HasMaxLength(100);
        entity.Property(e => e.BalanceColumn).HasMaxLength(100);
        entity.Property(e => e.DateFormat).HasMaxLength(100);
    }
}
