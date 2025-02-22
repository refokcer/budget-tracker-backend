using Microsoft.AspNetCore.Mvc;
using budget_tracker_backend.Dto.Transactions;
using budget_tracker_backend.MediatR.Transactions.Commands.Create;
using budget_tracker_backend.MediatR.Transactions.Queries.GetTransactions;
using budget_tracker_backend.Models.Enums;

namespace budget_tracker_backend.Controllers;

[Route("api/[controller]")]
[ApiController]
public class IncomeController : BaseApiController
{
    [HttpGet]
    public async Task<IActionResult> GetAllIncome()
    {
        var query = new GetTransactionsQuery(
            Type: TransactionCategoryType.Income,
            StartDate: null,
            EndDate: null
        );
        var result = await Mediator.Send(query);
        return HandleResult(result);
    }

    [HttpGet("filter")]
    public async Task<IActionResult> GetIncomeInDateRange([FromQuery] DateTime start, [FromQuery] DateTime end)
    {
        var query = new GetTransactionsQuery(
            Type: TransactionCategoryType.Income,
            StartDate: start,
            EndDate: end
        );
        var result = await Mediator.Send(query);
        return HandleResult(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateIncome([FromBody] CreateTransactionDto dto)
    {
        // Принудительно ставим Type=Income
        dto.Type = TransactionCategoryType.Income;

        var command = new CreateTransactionCommand(dto);
        var result = await Mediator.Send(command);
        return HandleResult(result);
    }
}
