using Microsoft.EntityFrameworkCore;
using BankService.Models;

namespace BankService.Data
{
    public class BankContext : DbContext
    {
        public BankContext(DbContextOptions<BankContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Account> Accounts { get; set; }
        public DbSet<Transaction> Transactions  { get; set; }

        // =====================================================
        // MEMBER 3 - PART A: SQLITE AUDIT TRAIL STORAGE
        // =====================================================
        // Stores administrator and system audit events.
        //
        // References:
        // https://learn.microsoft.com/ef/core/providers/sqlite/
        // https://learn.microsoft.com/ef/core/modeling/
        public DbSet<AuditLog> AuditLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Account>()
                .Property(a => a.Balance)
                .HasColumnType("NUMERIC");

            modelBuilder.Entity<Account>()
                .HasOne(a => a.User)
                .WithMany(u => u.Accounts)
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Transaction>()
                .HasOne(t => t.Account)
                .WithMany(u => u.SentTransactions)
                .HasForeignKey(t => t.AccountId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Transaction>()
                .Property(t => t.Amount)
                .HasColumnType("NUMERIC");

            modelBuilder.Entity<Transaction>()
                .HasOne(t => t.TargetAccount)
                .WithMany(a => a.ReceivedTransactions)
                .HasForeignKey(t => t.TargetAccountId)
                .OnDelete(DeleteBehavior.Restrict);

            // MEMBER 3 - PART A: Defaults new and migrated users
            // to active so access control can use the IsActive flag.
            modelBuilder.Entity<User>()
                .Property(u => u.IsActive)
                .HasDefaultValue(true);

            // MEMBER 3 - PART A: Indexes support efficient audit
            // filtering and administrator dashboard review.
            modelBuilder.Entity<AuditLog>()
                .HasIndex(a => a.OccurredAtUtc);

            modelBuilder.Entity<AuditLog>()
                .HasIndex(a => a.ActorUserId);

            modelBuilder.Entity<AuditLog>()
                .HasIndex(a => new { a.EntityType, a.EntityId });
        }
    }
    }

    

