using budget_tracker_backend.Controllers.Interfaces;
using budget_tracker_backend.Dto.BudgetPlans;
using budget_tracker_backend.MediatR.BudgetPlans.Commands.Create;
using budget_tracker_backend.MediatR.BudgetPlans.Commands.Delete;
using budget_tracker_backend.MediatR.BudgetPlans.Commands.Update;
using budget_tracker_backend.MediatR.BudgetPlans.Queries.GetAll;
using budget_tracker_backend.MediatR.BudgetPlans.Queries.GetById;
using budget_tracker_backend.MediatR.BudgetPlans.Queries.GetEvents;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace budget_tracker_backend.Controllers;

[Authorize]
[Route("api/[controller]")]
[ApiController]
public class BudgetPlansController : BaseApiController
{
    // GET: api/BudgetPlans
    [HttpGet]
    public async Task<IActionResult> GetBudgetPlans()
    {
        var result = await Mediator.Send(new GetAllBudgetPlansQuery());
        return HandleResult(result);
    }

    [HttpGet("events")]
    public async Task<IActionResult> GetEvents()
    {
        var result = await Mediator.Send(new GetAllEventsQuery());
        return HandleResult(result);
    }

    // GET: api/BudgetPlans/5
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetBudgetPlan(int id)
    {
        var result = await Mediator.Send(new GetBudgetPlanByIdQuery(id));
        return HandleResult(result);
    }

    // POST: api/BudgetPlans
    [HttpPost]
    public async Task<IActionResult> PostBudgetPlan([FromBody] CreateBudgetPlanDto dto)
    {
        var result = await Mediator.Send(new CreateBudgetPlanCommand(dto));
        return HandleResult(result);
    }

    // PUT: api/BudgetPlans
    [HttpPut]
    public async Task<IActionResult> PutBudgetPlan([FromBody] BudgetPlanDto dto)
    {
        var result = await Mediator.Send(new UpdateBudgetPlanCommand(dto));
        return HandleResult(result);
    }

    // DELETE: api/BudgetPlans/5
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteBudgetPlan(int id)
    {
        var result = await Mediator.Send(new DeleteBudgetPlanCommand(id));
        return HandleResult(result);
    }
}