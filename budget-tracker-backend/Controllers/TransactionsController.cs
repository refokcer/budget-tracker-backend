using Microsoft.AspNetCore.Mvc;
using budget_tracker_backend.Dto.Transactions;
using budget_tracker_backend.MediatR.Transactions.Commands.Create;
using budget_tracker_backend.MediatR.Transactions.Commands.Delete;
using budget_tracker_backend.MediatR.Transactions.Commands.Update;
using budget_tracker_backend.MediatR.Transactions.Queries.GetAll;
using budget_tracker_backend.MediatR.Transactions.Queries.GetById;

namespace budget_tracker_backend.Controllers;

[Route("api/[controller]")]
[ApiController]
public class TransactionsController : BaseApiController
{
    [HttpGet]
    public async Task<IActionResult> GetTransactions()
    {
        var result = await Mediator.Send(new GetAllTransactionsQuery());
        return HandleResult(result);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetTransaction(int id)
    {
        var result = await Mediator.Send(new GetTransactionByIdQuery(id));
        return HandleResult(result);
    }

    [HttpPost]
    public async Task<IActionResult> PostTransaction([FromBody] CreateTransactionDto dto)
    {
        var command = new CreateTransactionCommand(dto);
        var result = await Mediator.Send(command);
        return HandleResult(result);
    }

    [HttpPut]
    public async Task<IActionResult> PutTransaction([FromBody] TransactionDto dto)
    {
        var command = new UpdateTransactionCommand(dto);
        var result = await Mediator.Send(command);
        return HandleResult(result);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteTransaction(int id)
    {
        var command = new DeleteTransactionCommand(id);
        var result = await Mediator.Send(command);
        return HandleResult(result);
    }
}