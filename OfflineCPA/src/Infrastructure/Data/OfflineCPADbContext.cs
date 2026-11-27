using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data;

public class OfflineCPADbContext : DbContext
{
    public OfflineCPADbContext(DbContextOptions<OfflineCPADbContext> options) : base(options)
    {
    }

    public DbSet<Account> Accounts { get; set; } = null!;
    public DbSet<Transaction> Transactions { get; set; } = null!;
    public DbSet<JournalEntry> JournalEntries { get; set; } = null!;
    public DbSet<Period> Periods { get; set; } = null!;
    public DbSet<Posting> Postings { get; set; } = null!;
    public DbSet<LedgerAuditRecord> LedgerAuditRecords { get; set; } = null!;
    public DbSet<ImportBatch> ImportBatches { get; set; } = null!;
    public DbSet<StagedRow> StagedRows { get; set; } = null!;
    public DbSet<CsvMappingProfile> CsvMappingProfiles { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfiguration(new ImportBatchConfiguration());
        modelBuilder.ApplyConfiguration(new StagedRowConfiguration());
        modelBuilder.ApplyConfiguration(new CsvMappingProfileConfiguration());

        modelBuilder.Entity<Account>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(150);
            entity.Property(e => e.Code).IsRequired().HasMaxLength(50);
            entity.HasIndex(e => e.Code).IsUnique();
        });

        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Description).IsRequired().HasMaxLength(500);
        });

        modelBuilder.Entity<Period>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Ignore(e => e.DomainEvents);
        });

        modelBuilder.Entity<JournalEntry>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Memo).IsRequired().HasMaxLength(500);

            entity.HasOne<Period>()
                .WithMany()
                .HasForeignKey(e => e.PeriodId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(e => e.Postings)
                .WithOne()
                .HasForeignKey(p => p.JournalEntryId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.Metadata.FindNavigation(nameof(JournalEntry.Postings))!
                .SetPropertyAccessMode(PropertyAccessMode.Field);

            entity.HasIndex(e => e.PeriodId);
            entity.HasIndex(e => e.Status);
            entity.Ignore(e => e.DomainEvents);
        });

        modelBuilder.Entity<Posting>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Amount).HasColumnType("numeric(18,2)");
            entity.Property(e => e.Memo).HasMaxLength(500);

            entity.HasOne(p => p.Account)
                .WithMany(a => a.Postings)
                .HasForeignKey(p => p.AccountId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => e.AccountId);
        });

        modelBuilder.Entity<LedgerAuditRecord>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.LocalUserId).IsRequired().HasMaxLength(100);
            entity.Property(e => e.EventType).IsRequired().HasMaxLength(40);
            entity.Property(e => e.Details).IsRequired().HasMaxLength(500);
        });
    }
}
