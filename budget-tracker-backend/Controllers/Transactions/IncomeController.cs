using Microsoft.AspNetCore.Mvc;
using budget_tracker_backend.Dto.Transactions;
using budget_tracker_backend.MediatR.Transactions.Commands.Create;
using budget_tracker_backend.MediatR.Transactions.Queries.GetTransactions;
using budget_tracker_backend.Models.Enums;
using budget_tracker_backend.Controllers.Interfaces;

namespace budget_tracker_backend.Controllers.Transactions;

[Route("api/[controller]")]
[ApiController]
public class IncomeController : BaseApiController
{
    [HttpGet]
    public async Task<IActionResult> GetAllIncome()
    {
        var query = new GetTransactionsQuery(
            Type: TransactionCategoryType.Income
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

    [HttpGet("event/{eventId:int}")]
    public async Task<IActionResult> GetIncomeByEvent(int eventId)
    {
        var query = new GetTransactionsQuery(
            Type: TransactionCategoryType.Income,
            EventId: eventId
        );
        var result = await Mediator.Send(query);
        return HandleResult(result);
    }

}
