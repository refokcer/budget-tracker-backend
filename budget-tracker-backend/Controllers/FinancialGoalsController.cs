using budget_tracker_backend.Controllers.Interfaces;
using budget_tracker_backend.Dto.FinancialGoals;
using budget_tracker_backend.MediatR.FinancialGoals.Commands.ApplyBudgetAdjustments;
using budget_tracker_backend.MediatR.FinancialGoals.Commands.Create;
using budget_tracker_backend.MediatR.FinancialGoals.Commands.Delete;
using budget_tracker_backend.MediatR.FinancialGoals.Commands.Update;
using budget_tracker_backend.MediatR.FinancialGoals.Queries.GetAll;
using budget_tracker_backend.MediatR.FinancialGoals.Queries.GetById;
using budget_tracker_backend.MediatR.FinancialGoals.Queries.GetForecast;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace budget_tracker_backend.Controllers;

[Authorize]
[Route("api/[controller]")]
[ApiController]
public class FinancialGoalsController : BaseApiController
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await Mediator.Send(new GetAllFinancialGoalsQuery());
        return HandleResult(result);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await Mediator.Send(new GetFinancialGoalByIdQuery(id));
        return HandleResult(result);
    }

    [HttpGet("{id:int}/forecast")]
    public async Task<IActionResult> GetForecast(int id)
    {
        var result = await Mediator.Send(new GetFinancialGoalForecastQuery(id));
        return HandleResult(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateFinancialGoalDto dto)
    {
        var result = await Mediator.Send(new CreateFinancialGoalCommand(dto));
        return HandleResult(result);
    }

    [HttpPut]
    public async Task<IActionResult> Update([FromBody] FinancialGoalDto dto)
    {
        var result = await Mediator.Send(new UpdateFinancialGoalCommand(dto));
        return HandleResult(result);
    }

    [HttpPost("{id:int}/apply-budget-adjustments")]
    public async Task<IActionResult> ApplyBudgetAdjustments(int id)
    {
        var result = await Mediator.Send(new ApplyBudgetAdjustmentsCommand(id));
        return HandleResult(result);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await Mediator.Send(new DeleteFinancialGoalCommand(id));
        return HandleResult(result);
    }
}
