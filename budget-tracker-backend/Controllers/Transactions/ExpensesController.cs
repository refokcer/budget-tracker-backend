using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using budget_tracker_backend.Dto.Transactions;
using budget_tracker_backend.MediatR.Transactions.Commands.Create;
using budget_tracker_backend.MediatR.Transactions.Queries.GetTransactions;
using budget_tracker_backend.Models.Enums;
using budget_tracker_backend.Controllers.Interfaces;

namespace budget_tracker_backend.Controllers.Transactions;

[Authorize]
[Route("api/[controller]")]
[ApiController]
public class ExpensesController : BaseApiController
{
    // GET: api/Expenses
    [HttpGet]
    public async Task<IActionResult> GetAllExpenses()
    {
        var query = new GetTransactionsQuery(
            Type: TransactionCategoryType.Expense
        );
        var result = await Mediator.Send(query);
        return HandleResult(result);
    }

    // GET: api/Expenses/filter?start=...&end=...
    [HttpGet("filter")]
    public async Task<IActionResult> GetExpensesInDateRange([FromQuery] DateTime start, [FromQuery] DateTime end)
    {
        var query = new GetTransactionsQuery(
            Type: TransactionCategoryType.Expense,
            StartDate: start,
            EndDate: end
        );
        var result = await Mediator.Send(query);
        return HandleResult(result);
    }

    // POST: api/Expenses
    [HttpPost]
    public async Task<IActionResult> CreateExpense([FromBody] CreateTransactionDto dto)
    {
        // Force Type=Expense
        dto.Type = TransactionCategoryType.Expense;

        var command = new CreateTransactionCommand(dto);
        var result = await Mediator.Send(command);
        return HandleResult(result);
    }

}
