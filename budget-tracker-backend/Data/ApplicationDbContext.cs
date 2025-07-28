using Microsoft.EntityFrameworkCore;
using budget_tracker_backend.Models;

namespace budget_tracker_backend.Data;

public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Category> Categories { get; set; }
    public DbSet<Transaction> Transactions { get; set; }
    public DbSet<Account> Accounts { get; set; }
    public DbSet<BudgetPlan> BudgetPlans { get; set; }
    public DbSet<BudgetPlanItem> BudgetPlanItems { get; set; }
    public DbSet<Currency> Currencies { get; set; }
    public DbSet<Event> Events { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Account>()
           .Property(a => a.Amount)
           .HasColumnType("decimal(18,4)");

        modelBuilder.Entity<BudgetPlanItem>()
            .Property(bp => bp.Amount)
            .HasColumnType("decimal(18,4)");

        modelBuilder.Entity<BudgetPlanItem>()
            .HasOne(i => i.BudgetPlan)
            .WithMany(p => p.Items)
            .HasForeignKey(i => i.BudgetPlanId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Transaction>()
            .Property(t => t.Amount)
            .HasColumnType("decimal(18,4)");

        modelBuilder.Entity<Transaction>()
            .Property(t => t.UnicCode)
            .HasMaxLength(64);

        modelBuilder.Entity<Transaction>()
            .HasIndex(t => t.UnicCode);

        // 1. Prohibit cascading deletion Event → Transaction
        modelBuilder.Entity<Transaction>()
            .HasOne(t => t.Event)
            .WithMany(e => e.Transactions)
            .HasForeignKey(t => t.EventId)
            .OnDelete(DeleteBehavior.Restrict);

        // 2. Allow deletion of Category (set NULL)
        modelBuilder.Entity<Transaction>()
            .HasOne(t => t.Category)
            .WithMany()
            .HasForeignKey(t => t.CategoryId)
            .OnDelete(DeleteBehavior.SetNull);

        // 3. Account deletion is prohibited if there are related Transactions
        modelBuilder.Entity<Transaction>()
            .HasOne(t => t.FromAccount)
            .WithMany()
            .HasForeignKey(t => t.AccountFrom)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Transaction>()
            .HasOne(t => t.ToAccount)
            .WithMany()
            .HasForeignKey(t => t.AccountTo)
            .OnDelete(DeleteBehavior.Restrict);

        // 4. Currency deletion is prohibited if there are related Transactions
        modelBuilder.Entity<Transaction>()
            .HasOne(t => t.Currency)
            .WithMany()
            .HasForeignKey(t => t.CurrencyId)
            .OnDelete(DeleteBehavior.Restrict);

        base.OnModelCreating(modelBuilder);
    }
}
