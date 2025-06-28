using Microsoft.AspNetCore.Mvc;
using budget_tracker_backend.MediatR.Pages.ExpensesByMonth;
using budget_tracker_backend.MediatR.Pages.IncomesByMonth;
using budget_tracker_backend.MediatR.Pages.TransfersByMonth;
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
}
