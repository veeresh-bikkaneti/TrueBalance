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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

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

        modelBuilder.Entity<JournalEntry>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Amount).HasColumnType("decimal(18,2)");
            
            entity.HasOne(d => d.Account)
                .WithMany(p => p.JournalEntries)
                .HasForeignKey(d => d.AccountId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.Transaction)
                .WithMany(p => p.Entries)
                .HasForeignKey(d => d.TransactionId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
