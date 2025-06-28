using Microsoft.AspNetCore.Mvc;
using budget_tracker_backend.MediatR.Pages.ExpensesByMonth;
using budget_tracker_backend.MediatR.Pages.IncomesByMonth;
using budget_tracker_backend.MediatR.Pages.TransfersByMonth;
using budget_tracker_backend.MediatR.Pages.BudgetPlanPage;
using budget_tracker_backend.MediatR.Pages.Dashboard;
using budget_tracker_backend.MediatR.Pages.MonthlyReport;
using budget_tracker_backend.Controllers.Interfaces;

namespace budget_tracker_backend.Controllers;

[Route("api/pages")]
[ApiController]
public class PagesController : BaseApiController
{
    /// <summary>
    /// Витрати за місяць.
    /// </summary>
    /// <param name="month">1–12</param>
    /// <param name="year">необов’язково, за замовчанням – поточний</param>
    [HttpGet("expensesByMonth/{month:int}")]
    public async Task<IActionResult> ExpensesByMonth(int month, [FromQuery] int? year)
    {
        var query = new GetExpensesByMonthQuery(month, year);
        var result = await Mediator.Send(query);
        return HandleResult(result);
    }

    /// <summary>
    /// Надходження за місяць.
    /// </summary>
    /// <param name="month">1–12</param>
    /// <param name="year">необов’язково, за замовчанням – поточний</param>
    [HttpGet("incomeByMonth/{month:int}")]
    public async Task<IActionResult> IncomeByMonth(int month, [FromQuery] int? year)
    {
        var query = new GetIncomesByMonthQuery(month, year);
        var result = await Mediator.Send(query);
        return HandleResult(result);
    }

    /// <summary>
    /// Перекази за місяць.
    /// </summary>
    /// <param name="month">1–12</param>
    /// <param name="year">необов’язково, за замовчанням – поточний</param>
    [HttpGet("transfersByMonth/{month:int}")]
    public async Task<IActionResult> TransfersByMonth(int month, [FromQuery] int? year)
    {
        var query = new GetTransfersByMonthQuery(month, year);
        var result = await Mediator.Send(query);
        return HandleResult(result);
    }

    [HttpGet("budgetPlanPage/{planId:int}")]
    public async Task<IActionResult> BudgetPlanPage(int planId)
    {
        var query = new GetBudgetPlanPageQuery(planId);
        var result = await Mediator.Send(query);
        return HandleResult(result);
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard()
    {
        var result = await Mediator.Send(new GetDashboardQuery());
        return HandleResult(result);
    }

    [HttpGet("monthlyReport/{month:int}")]
    public async Task<IActionResult> MonthlyReport(int month, [FromQuery] int? year)
    {
        var query = new GetMonthlyReportQuery(month, year);
        var result = await Mediator.Send(query);
        return HandleResult(result);
    }
}
