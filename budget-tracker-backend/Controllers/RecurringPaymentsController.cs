using budget_tracker_backend.Controllers.Interfaces;
using budget_tracker_backend.Dto.RecurringPayments;
using budget_tracker_backend.Services.RecurringPayments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace budget_tracker_backend.Controllers;

[Authorize]
[Route("api/[controller]")]
[ApiController]
public class RecurringPaymentsController : BaseApiController
{
    private readonly IRecurringPaymentManager _manager;

    public RecurringPaymentsController(IRecurringPaymentManager manager)
    {
        _manager = manager;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await _manager.GetAllAsync(cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var result = await _manager.GetByIdAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpGet("options")]
    public async Task<IActionResult> GetOptions(CancellationToken cancellationToken)
    {
        var result = await _manager.GetOptionsAsync(cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateRecurringPaymentDto dto, CancellationToken cancellationToken)
    {
        var result = await _manager.CreateAsync(dto, cancellationToken);
        return Ok(result);
    }

    [HttpPut]
    public async Task<IActionResult> Update([FromBody] UpdateRecurringPaymentDto dto, CancellationToken cancellationToken)
    {
        var result = await _manager.UpdateAsync(dto, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var result = await _manager.DeleteAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpGet("projected")]
    public async Task<IActionResult> GetProjected(
        [FromQuery] DateTime start,
        [FromQuery] DateTime end,
        CancellationToken cancellationToken)
    {
        var result = await _manager.GetProjectedOccurrencesAsync(start, end, cancellationToken);
        return Ok(result);
    }

    [HttpPost("generate-due")]
    public async Task<IActionResult> GenerateDue(
        [FromBody] GenerateRecurringPaymentsDto dto,
        CancellationToken cancellationToken)
    {
        var result = await _manager.GenerateDueTransactionsAsync(dto.UpTo ?? DateTime.UtcNow.Date, cancellationToken);
        return Ok(result);
    }
}
