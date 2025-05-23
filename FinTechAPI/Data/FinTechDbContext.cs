// FinTechDbContext.cs
using FinTechAPI.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FinTechAPI.Data
{
    public class FinTechDbContext : IdentityDbContext<User>
    {
        public FinTechDbContext(DbContextOptions<FinTechDbContext> options)
            : base(options)
        {
        }
        
        public virtual DbSet<Account> Accounts { get; set; } // Make this property virtual
        
        public virtual DbSet<Transaction> Transactions { get; set; } // Also make other DbSets virtual if you mock them

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure relationships and constraints
            modelBuilder.Entity<Account>()
                .HasOne(a => a.User)
                .WithMany(u => u.Accounts)
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Transaction>()
                .HasOne(t => t.Account)
                .WithMany(a => a.Transactions)
                .HasForeignKey(t => t.AccountId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Transaction>()
                .HasOne(t => t.User)
                .WithMany(u => u.Transactions)
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.NoAction); // To avoid circular cascade delete

            // Explicitly specify precision and scale for decimal properties
            modelBuilder.Entity<Account>()
                .Property(a => a.Balance)
                .HasColumnType("decimal")
                .HasPrecision(18, 2);

            modelBuilder.Entity<Transaction>()
                .Property(t => t.Amount)
                .HasColumnType("decimal")
                .HasPrecision(18, 2);
        }

    }
}