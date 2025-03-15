using Microsoft.AspNetCore.Mvc;
using budget_tracker_backend.Dto.BudgetPlanItems;
using budget_tracker_backend.MediatR.BudgetPlanItems.Commands.Create;
using budget_tracker_backend.MediatR.BudgetPlanItems.Commands.Delete;
using budget_tracker_backend.MediatR.BudgetPlanItems.Commands.Update;
using budget_tracker_backend.MediatR.BudgetPlanItems.Queries.GetAll;
using budget_tracker_backend.MediatR.BudgetPlanItems.Queries.GetById;
using budget_tracker_backend.Controllers.Interfaces;
using budget_tracker_backend.MediatR.BudgetPlanItems.Queries.GetByPlanId;

namespace budget_tracker_backend.Controllers;

[Route("api/[controller]")]
[ApiController]
public class BudgetPlanItemsController : BaseApiController
{
    // GET: api/BudgetPlanItems
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await Mediator.Send(new GetAllBudgetPlanItemsQuery());
        return HandleResult(result);
    }

    // GET: api/BudgetPlanItems/5
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await Mediator.Send(new GetBudgetPlanItemByIdQuery(id));
        return HandleResult(result);
    }

    // POST: api/BudgetPlanItems
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateBudgetPlanItemDto dto)
    {
        var command = new CreateBudgetPlanItemCommand(dto);
        var result = await Mediator.Send(command);
        return HandleResult(result);
    }

    // PUT: api/BudgetPlanItems
    [HttpPut]
    public async Task<IActionResult> Update([FromBody] BudgetPlanItemDto dto)
    {
        var command = new UpdateBudgetPlanItemCommand(dto);
        var result = await Mediator.Send(command);
        return HandleResult(result);
    }

    // DELETE: api/BudgetPlanItems/5
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var command = new DeleteBudgetPlanItemCommand(id);
        var result = await Mediator.Send(command);
        return HandleResult(result);
    }

    [HttpGet("byPlan/{planId:int}")]
    public async Task<IActionResult> GetAllByPlanId(int planId)
    {
        var query = new GetAllBudgetPlanItemsByPlanIdQuery(planId);
        var result = await Mediator.Send(query);
        return HandleResult(result);
    }
}