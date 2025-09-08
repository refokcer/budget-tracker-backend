namespace budget_tracker_backend.Services.Pages;

using budget_tracker_backend.Dto.Pages;
using budget_tracker_backend.Models.Enums;

public interface IPageManager
{
    Task<DashboardDto> GetDashboardAsync(CancellationToken cancellationToken);
    Task<BudgetPlanPageDto> GetBudgetPlanPageAsync(int planId, bool includeEvents, CancellationToken cancellationToken);
    Task<BudgetPlanEventDto> GetEventPageAsync(int eventId, CancellationToken cancellationToken);
    Task<IncomesByMonthDto> GetIncomesByMonthAsync(int month, int? year, CancellationToken cancellationToken);
    Task<ExpensesByMonthDto> GetExpensesByMonthAsync(int month, int? year, CancellationToken cancellationToken);
    Task<TransfersByMonthDto> GetTransfersByMonthAsync(int month, int? year, CancellationToken cancellationToken);
    Task<MonthlyReportDto> GetMonthlyReportAsync(int month, int? year, CancellationToken cancellationToken);
}
