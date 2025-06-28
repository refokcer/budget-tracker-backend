using Microsoft.AspNetCore.Mvc;
using budget_tracker_backend.Controllers.Interfaces;
using budget_tracker_backend.MediatR.Components.IncomeModal;
using budget_tracker_backend.MediatR.Components.ExpenseModal;
using budget_tracker_backend.MediatR.Components.TransferModal;

namespace budget_tracker_backend.Controllers;

[Route("api/components")]
[ApiController]
public class ComponentsController : BaseApiController
{
    [HttpGet("incomeModal")]
    public async Task<IActionResult> IncomeModal()
    {
        var query = new GetIncomeModalQuery();
        var result = await Mediator.Send(query);
        return HandleResult(result);
    }

    [HttpGet("expenseModal")]
    public async Task<IActionResult> ExpenseModal()
    {
        var query = new GetExpenseModalQuery();
        var result = await Mediator.Send(query);
        return HandleResult(result);
    }

    [HttpGet("transferModal")]
    public async Task<IActionResult> TransferModal()
    {
        var query = new GetTransferModalQuery();
        var result = await Mediator.Send(query);
        return HandleResult(result);
    }
}
