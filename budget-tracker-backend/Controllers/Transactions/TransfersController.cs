using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using budget_tracker_backend.Dto.Transactions;
using budget_tracker_backend.MediatR.Transactions.Commands.Create;
using budget_tracker_backend.MediatR.Transactions.Commands.Delete;
using budget_tracker_backend.MediatR.Transactions.Commands.Update;
using budget_tracker_backend.MediatR.Transactions.Queries.GetById;
using budget_tracker_backend.MediatR.Transactions.Queries.GetByFilterFull;
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
            Type: TransactionCategoryType.Transfer,
            StartDate: null,
            EndDate: null
        );
        var result = await Mediator.Send(query);
        return HandleResult(result);
    }

    [HttpGet("filter")]
    public async Task<IActionResult> GetTransfersInDateRange([FromQuery] DateTime start, [FromQuery] DateTime end)
    {
        var query = new GetTransactionsByFilterFullQuery(
            Type: TransactionCategoryType.Transfer,
            CategoryId: null,
            StartDate: start,
            EndDate: end,
            BudgetPlanId: null,
            AccountFrom: null,
            AccountTo: null);
        var result = await Mediator.Send(query);
        return HandleResult(result);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetTransferById(int id)
    {
        var query = new GetTransactionByIdQuery(id);
        var result = await Mediator.Send(query);
        if (result.IsSuccess && result.Value?.Type != TransactionCategoryType.Transfer)
            return NotFound();

        return HandleResult(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateTransfer([FromBody] CreateTransactionDto dto)
    {
        dto.Type = TransactionCategoryType.Transfer;

        var command = new CreateTransactionCommand(dto);
        var result = await Mediator.Send(command);
        return HandleResult(result);
    }

    [HttpPut]
    public async Task<IActionResult> UpdateTransfer([FromBody] UpdateTransactionDto dto)
    {
        var existing = await Mediator.Send(new GetTransactionByIdQuery(dto.Id));
        if (existing.IsSuccess && existing.Value?.Type != TransactionCategoryType.Transfer)
            return NotFound();

        if (existing.IsFailed)
            return HandleResult(existing);

        dto.Type = TransactionCategoryType.Transfer;

        var command = new UpdateTransactionCommand(dto);
        var result = await Mediator.Send(command);
        return HandleResult(result);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteTransfer(int id)
    {
        var query = new GetTransactionByIdQuery(id);
        var existing = await Mediator.Send(query);
        if (existing.IsSuccess && existing.Value?.Type != TransactionCategoryType.Transfer)
            return NotFound();

        if (existing.IsFailed)
            return HandleResult(existing);

        var command = new DeleteTransactionCommand(id);
        var result = await Mediator.Send(command);
        return HandleResult(result);
    }
}
