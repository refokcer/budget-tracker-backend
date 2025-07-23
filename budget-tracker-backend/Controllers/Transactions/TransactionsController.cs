using Microsoft.AspNetCore.Mvc;
using budget_tracker_backend.Dto.Transactions;
using budget_tracker_backend.MediatR.Transactions.Commands.Delete;
using budget_tracker_backend.MediatR.Transactions.Commands.Update;
using budget_tracker_backend.MediatR.Transactions.Queries.GetAll;
using budget_tracker_backend.MediatR.Transactions.Queries.GetById;
using budget_tracker_backend.Controllers.Interfaces;
using budget_tracker_backend.MediatR.Transactions.Queries.GetTransactions;
using budget_tracker_backend.MediatR.Transactions.Queries.GetByEvent;
using budget_tracker_backend.MediatR.Transactions.Queries.GetByBudgetPlan;

namespace budget_tracker_backend.Controllers.Transactions;

[Route("api/[controller]")]
[ApiController]
public class TransactionsController : BaseApiController
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var query = new GetAllTransactionsQuery();
        var result = await Mediator.Send(query);
        return HandleResult(result);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var query = new GetTransactionByIdQuery(id);
        var result = await Mediator.Send(query);
        return HandleResult(result);
    }

    [HttpPut]
    public async Task<IActionResult> Update([FromBody] UpdateTransactionDto dto)
    {
        var command = new UpdateTransactionCommand(dto);
        var result = await Mediator.Send(command);
        return HandleResult(result);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var command = new DeleteTransactionCommand(id);
        var result = await Mediator.Send(command);
        return HandleResult(result);
    }

    [HttpGet("event/{eventId:int}")]
    public async Task<IActionResult> GetTransactionsByEvent(int eventId)
    {
        var query = new GetTransactionsQuery(
            EventId: eventId
        );
        var result = await Mediator.Send(query);
        return HandleResult(result);
    }

    [HttpGet("byEvent/{eventId:int}")]
    public async Task<IActionResult> GetAllByEvent(int eventId)
    {
        var query = new GetAllTransactionsByEventQuery(eventId);
        var result = await Mediator.Send(query);
        return HandleResult(result);
    }

    // GET: /api/Transactions/byPlan/5
    [HttpGet("byPlan/{planId:int}")]
    public async Task<IActionResult> GetByBudgetPlan(int planId)
    {
        var query = new GetTransactionsByBudgetPlanIdQuery(planId);
        var result = await Mediator.Send(query);
        return HandleResult(result);
    }
}
