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

    private string? CurrentUserId =>
        _ctx.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);

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

        modelBuilder.Entity<Account>(b =>
        {
            b.Property(a => a.Amount).HasColumnType("decimal(18,4)");
            b.HasQueryFilter(a => a.UserId == CurrentUserId);
            b.HasIndex(a => a.UserId);
            b.HasOne(a => a.User)
                .WithMany()
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Category>(b =>
        {
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
        });

        modelBuilder.Entity<Event>(b =>
        {
            b.HasQueryFilter(e => e.UserId == CurrentUserId);
            b.HasIndex(e => e.UserId);
            b.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
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
