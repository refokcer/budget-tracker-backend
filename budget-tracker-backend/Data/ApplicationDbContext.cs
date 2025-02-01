using Microsoft.EntityFrameworkCore;
using budget_tracker_backend.Models;

namespace budget_tracker_backend.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Category> Categories { get; set; }
    public DbSet<Transaction> Transactions { get; set; }
    public DbSet<Account> Accounts { get; set; }
    public DbSet<BudgetPlan> BudgetPlans { get; set; }
    public DbSet<Currency> Currencies { get; set; }
    public DbSet<Event> Events { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Account>()
           .Property(a => a.Amount)
           .HasColumnType("decimal(18,4)");

        modelBuilder.Entity<BudgetPlan>()
            .Property(bp => bp.Amount)
            .HasColumnType("decimal(18,4)");

        modelBuilder.Entity<Transaction>()
            .Property(t => t.Amount)
            .HasColumnType("decimal(18,4)");

        // 1. Запрещаем каскадное удаление Event → Transaction
        modelBuilder.Entity<Transaction>()
            .HasOne(t => t.Event)
            .WithMany(e => e.Transactions)
            .HasForeignKey(t => t.EventId)
            .OnDelete(DeleteBehavior.Restrict); // ОТКЛЮЧАЕМ CASCADE

        // 2. Разрешаем удаление Category (ставим NULL)
        modelBuilder.Entity<Transaction>()
            .HasOne(t => t.Category)
            .WithMany()
            .HasForeignKey(t => t.CategoryId)
            .OnDelete(DeleteBehavior.SetNull);

        // 3. Удаление Account запрещаем, если есть связанные Transactions
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

        // 4. Удаление Currency запрещаем, если есть связанные Transactions
        modelBuilder.Entity<Transaction>()
            .HasOne(t => t.Currency)
            .WithMany()
            .HasForeignKey(t => t.CurrencyId)
            .OnDelete(DeleteBehavior.Restrict);

        base.OnModelCreating(modelBuilder);
    }
}
