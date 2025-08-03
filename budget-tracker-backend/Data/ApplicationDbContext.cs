using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using budget_tracker_backend.Models;
using System.Security.Claims;
using System.Linq;
using System.Threading;

namespace budget_tracker_backend.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>, IApplicationDbContext
{
    private readonly IHttpContextAccessor _ctx;

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, IHttpContextAccessor ctx) : base(options)
    {
        _ctx = ctx;
    }

    public DbSet<Category> Categories { get; set; }
    public DbSet<Transaction> Transactions { get; set; }
    public DbSet<Account> Accounts { get; set; }
    public DbSet<BudgetPlan> BudgetPlans { get; set; }
    public DbSet<BudgetPlanItem> BudgetPlanItems { get; set; }
    public DbSet<Currency> Currencies { get; set; }
    public DbSet<Event> Events { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        var userId = _ctx.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);

        modelBuilder.Entity<Account>(b =>
        {
            b.Property(a => a.Amount).HasColumnType("decimal(18,4)");
            b.HasQueryFilter(a => a.UserId == userId);
            b.HasIndex(a => a.UserId);
        });

        modelBuilder.Entity<Category>(b =>
        {
            b.HasQueryFilter(c => c.UserId == userId);
            b.HasIndex(c => c.UserId);
        });

        modelBuilder.Entity<BudgetPlan>(b =>
        {
            b.HasQueryFilter(p => p.UserId == userId);
            b.HasIndex(p => p.UserId);
        });

        modelBuilder.Entity<Event>(b =>
        {
            b.HasQueryFilter(e => e.UserId == userId);
            b.HasIndex(e => e.UserId);
        });

        modelBuilder.Entity<BudgetPlanItem>(b =>
        {
            b.Property(bp => bp.Amount).HasColumnType("decimal(18,4)");
            b.HasOne(i => i.BudgetPlan)
                .WithMany(p => p.Items)
                .HasForeignKey(i => i.BudgetPlanId)
                .OnDelete(DeleteBehavior.Cascade);
            b.HasQueryFilter(i => i.BudgetPlan!.UserId == userId);
        });

        modelBuilder.Entity<Transaction>(b =>
        {
            b.Property(t => t.Amount).HasColumnType("decimal(18,4)");
            b.Property(t => t.UnicCode).HasMaxLength(64);
            b.HasIndex(t => t.UnicCode);
            b.HasQueryFilter(t => t.UserId == userId);
            b.HasIndex(t => new { t.UserId, t.Date });

            b.HasOne(t => t.Event)
                .WithMany(e => e.Transactions)
                .HasForeignKey(t => t.EventId)
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
        var userId = _ctx.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);

        foreach (var entry in ChangeTracker.Entries<IUserOwnedEntity>().Where(e => e.State == EntityState.Added))
        {
            if (string.IsNullOrEmpty(entry.Entity.UserId) && userId != null)
            {
                entry.Entity.UserId = userId;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
