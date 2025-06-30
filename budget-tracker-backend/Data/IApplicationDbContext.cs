using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using budget_tracker_backend.Models;

namespace budget_tracker_backend.Data;

public interface IApplicationDbContext
{
    DbSet<Category> Categories { get; }
    DbSet<Transaction> Transactions { get; }
    DbSet<Account> Accounts { get; }
    DbSet<BudgetPlan> BudgetPlans { get; }
    DbSet<BudgetPlanItem> BudgetPlanItems { get; }
    DbSet<Currency> Currencies { get; }
    DbSet<Event> Events { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
