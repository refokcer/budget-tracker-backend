using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using budget_tracker_backend.Models;
using budget_tracker_backend.Models.Enums;
using System.Security.Claims;
using System.Linq;
using System.Threading;

namespace budget_tracker_backend.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>, IApplicationDbContext
{
    private readonly IHttpContextAccessor _ctx;

    private string? CurrentUserId =>
        _ctx.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, IHttpContextAccessor ctx) : base(options)
    {
        _ctx = ctx;
    }

    public DbSet<Category> Categories { get; set; }
    public DbSet<Transaction> Transactions { get; set; }
    public DbSet<Account> Accounts { get; set; }
    public DbSet<FinancialGoal> FinancialGoals { get; set; }
    public DbSet<RecurringPayment> RecurringPayments { get; set; }
    public DbSet<BudgetPlan> BudgetPlans { get; set; }
    public DbSet<BudgetPlanItem> BudgetPlanItems { get; set; }
    public DbSet<Currency> Currencies { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Account>(b =>
        {
            b.Property(a => a.Amount).HasColumnType("decimal(18,4)");
            b.Property(a => a.Type)
                .HasConversion<string>()
                .HasMaxLength(32)
                .HasDefaultValue(AccountType.Other);
            b.HasQueryFilter(a => a.UserId == CurrentUserId);
            b.HasIndex(a => a.UserId);
            b.HasOne(a => a.User)
                .WithMany()
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Category>(b =>
        {
            b.Property(c => c.Color).HasMaxLength(7);
            b.Property(c => c.Priority)
                .HasConversion<string>()
                .HasMaxLength(32)
                .HasDefaultValue(CategoryPriority.Flexible);
            b.HasQueryFilter(c => c.UserId == CurrentUserId);
            b.HasIndex(c => c.UserId);
            b.HasOne(c => c.User)
                .WithMany()
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<BudgetPlan>(b =>
        {
            b.HasQueryFilter(p => p.UserId == CurrentUserId);
            b.HasIndex(p => p.UserId);
            b.HasOne(p => p.User)
                .WithMany()
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Restrict);
            b.HasOne(p => p.Parent)
                .WithMany()
                .HasForeignKey(p => p.ParentId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<FinancialGoal>(b =>
        {
            b.Property(g => g.TargetAmount).HasColumnType("decimal(18,4)");
            b.Property(g => g.InitialAmount).HasColumnType("decimal(18,4)");
            b.HasQueryFilter(g => g.UserId == CurrentUserId);
            b.HasIndex(g => g.UserId);
            b.HasIndex(g => g.TargetDate);
            b.HasOne(g => g.User)
                .WithMany()
                .HasForeignKey(g => g.UserId)
                .OnDelete(DeleteBehavior.Restrict);
            b.HasOne(g => g.LinkedAccount)
                .WithMany()
                .HasForeignKey(g => g.LinkedAccountId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<RecurringPayment>(b =>
        {
            b.Property(p => p.Title).HasMaxLength(180);
            b.Property(p => p.Amount).HasColumnType("decimal(18,4)");
            b.Property(p => p.Frequency)
                .HasConversion<string>()
                .HasMaxLength(32);
            b.Property(p => p.Type)
                .HasConversion<string>()
                .HasMaxLength(32);
            b.Property(p => p.Description).HasMaxLength(512);
            b.HasQueryFilter(p => p.UserId == CurrentUserId);
            b.HasIndex(p => p.UserId);
            b.HasIndex(p => new { p.UserId, p.IsActive, p.StartDate });
            b.HasOne(p => p.User)
                .WithMany()
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Restrict);
            b.HasOne(p => p.Category)
                .WithMany()
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.SetNull);
            b.HasOne(p => p.Currency)
                .WithMany()
                .HasForeignKey(p => p.CurrencyId)
                .OnDelete(DeleteBehavior.Restrict);
            b.HasOne(p => p.FromAccount)
                .WithMany()
                .HasForeignKey(p => p.AccountFrom)
                .OnDelete(DeleteBehavior.Restrict);
            b.HasOne(p => p.ToAccount)
                .WithMany()
                .HasForeignKey(p => p.AccountTo)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<BudgetPlanItem>(b =>
        {
            b.Property(bp => bp.Amount).HasColumnType("decimal(18,4)");
            b.HasOne(i => i.BudgetPlan)
                .WithMany(p => p.Items)
                .HasForeignKey(i => i.BudgetPlanId)
                .OnDelete(DeleteBehavior.Cascade);
            b.HasQueryFilter(i => i.BudgetPlan!.UserId == CurrentUserId);
        });

        modelBuilder.Entity<Transaction>(b =>
        {
            b.Property(t => t.Amount).HasColumnType("decimal(18,4)");
            b.Property(t => t.UnicCode).HasMaxLength(64);
            b.HasIndex(t => t.UnicCode);
            b.HasQueryFilter(t => t.UserId == CurrentUserId);
            b.HasIndex(t => new { t.UserId, t.Date });

            b.HasOne(t => t.User)
                .WithMany()
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(t => t.Category)
                .WithMany()
                .HasForeignKey(t => t.CategoryId)
                .OnDelete(DeleteBehavior.SetNull);

            b.HasOne(t => t.FromAccount)
                .WithMany()
                .HasForeignKey(t => t.AccountFrom)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(t => t.ToAccount)
                .WithMany()
                .HasForeignKey(t => t.AccountTo)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(t => t.Currency)
                .WithMany()
                .HasForeignKey(t => t.CurrencyId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<IUserOwnedEntity>().Where(e => e.State == EntityState.Added))
        {
            if (string.IsNullOrEmpty(entry.Entity.UserId) && CurrentUserId != null)
            {
                entry.Entity.UserId = CurrentUserId;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
