using Microsoft.AspNetCore.Mvc;
using budget_tracker_backend.Controllers.Interfaces;
using budget_tracker_backend.MediatR.Components.IncomeModal;
using budget_tracker_backend.MediatR.Components.ExpenseModal;
using budget_tracker_backend.MediatR.Components.TransferModal;
using budget_tracker_backend.MediatR.Components.EditPlanModal;
using budget_tracker_backend.MediatR.Components.ManageAccounts;
using budget_tracker_backend.MediatR.Components.ManageCategories;
using budget_tracker_backend.Models.Enums;

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

    [HttpGet("editPlanModal")]
    public async Task<IActionResult> EditPlanModal()
    {
        var query = new GetEditPlanModalQuery();
        var result = await Mediator.Send(query);
        return HandleResult(result);
    }

    [HttpGet("manageAccounts")]
    public async Task<IActionResult> ManageAccounts()
    {
        var query = new GetManageAccountsQuery();
        var result = await Mediator.Send(query);
        return HandleResult(result);
    }

    [HttpGet("manageCategories/{type}")]
    public async Task<IActionResult> ManageCategories(string type)
    {
        if (!Enum.TryParse<TransactionCategoryType>(type, true, out var txType))
            return BadRequest("Invalid type");

        var query = new GetManageCategoriesQuery(txType);
        var result = await Mediator.Send(query);
        return HandleResult(result);
    }
}
