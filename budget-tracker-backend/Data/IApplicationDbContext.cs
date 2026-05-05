using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using budget_tracker_backend.Models;

namespace budget_tracker_backend.Data;

public interface IApplicationDbContext
{
    DbSet<Category> Categories { get; }
    DbSet<Transaction> Transactions { get; }
    DbSet<Account> Accounts { get; }
    DbSet<FinancialGoal> FinancialGoals { get; }
    DbSet<BudgetPlan> BudgetPlans { get; }
    DbSet<BudgetPlanItem> BudgetPlanItems { get; }
    DbSet<Currency> Currencies { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<ApplicationUser> Users { get; }
    DbSet<IdentityUserClaim<string>> UserClaims { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
