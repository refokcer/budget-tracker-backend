using Microsoft.AspNetCore.Mvc;
using budget_tracker_backend.Dto.Transactions;
using budget_tracker_backend.MediatR.Transactions.Commands.Delete;
using budget_tracker_backend.MediatR.Transactions.Commands.Update;
using budget_tracker_backend.MediatR.Transactions.Queries.GetAll;
using budget_tracker_backend.MediatR.Transactions.Queries.GetById;

namespace budget_tracker_backend.Controllers;

[Route("api/[controller]")]
[ApiController]
public class TransactionsController : BaseApiController
{
    // 1. Получить все транзакции (любого типа)
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var query = new GetAllTransactionsQuery();
        var result = await Mediator.Send(query);
        return HandleResult(result);
    }

    // 2. Получить транзакцию по Id
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var query = new GetTransactionByIdQuery(id);
        var result = await Mediator.Send(query);
        return HandleResult(result);
    }

    // 3. Обновить транзакцию (PUT) - может менять поля, в т.ч. Type
    [HttpPut]
    public async Task<IActionResult> Update([FromBody] TransactionDto dto)
    {
        var command = new UpdateTransactionCommand(dto);
        var result = await Mediator.Send(command);
        return HandleResult(result);
    }

    // 4. Удалить транзакцию
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var command = new DeleteTransactionCommand(id);
        var result = await Mediator.Send(command);
        return HandleResult(result);
    }
}
