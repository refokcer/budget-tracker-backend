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
public class TransfersController : BaseApiController
{
    [HttpGet]
    public async Task<IActionResult> GetAllTransfers()
    {
        var query = new GetTransactionsQuery(
            Type: TransactionCategoryType.Transaction,
            StartDate: null,
            EndDate: null
        );
        var result = await Mediator.Send(query);
        return HandleResult(result);
    }

    [HttpGet("filter")]
    public async Task<IActionResult> GetTransfersInDateRange([FromQuery] DateTime start, [FromQuery] DateTime end)
    {
        var query = new GetTransactionsQuery(
            Type: TransactionCategoryType.Transaction,
            StartDate: start,
            EndDate: end
        );
        var result = await Mediator.Send(query);
        return HandleResult(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateTransfer([FromBody] CreateTransactionDto dto)
    {
        // Force Type=Transaction
        dto.Type = TransactionCategoryType.Transaction;

        var command = new CreateTransactionCommand(dto);
        var result = await Mediator.Send(command);
        return HandleResult(result);
    }

}