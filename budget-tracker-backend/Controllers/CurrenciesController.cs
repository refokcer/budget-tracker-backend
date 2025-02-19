using Microsoft.AspNetCore.Mvc;
using budget_tracker_backend.Dto.Currencies;
using budget_tracker_backend.MediatR.Currencies.Commands.Create;
using budget_tracker_backend.MediatR.Currencies.Commands.Delete;
using budget_tracker_backend.MediatR.Currencies.Commands.Update;
using budget_tracker_backend.MediatR.Currencies.Queries.GetAll;
using budget_tracker_backend.MediatR.Currencies.Queries.GetById;
using Streetcode.WebApi.Controllers;

namespace budget_tracker_backend.Controllers;

[Route("api/[controller]")]
[ApiController]
public class CurrenciesController : BaseApiController
{
    // GET: api/Currencies
    [HttpGet]
    public async Task<IActionResult> GetCurrencies()
    {
        var result = await Mediator.Send(new GetAllCurrenciesQuery());
        return HandleResult(result);
    }

    // GET: api/Currencies/5
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetCurrency(int id)
    {
        var result = await Mediator.Send(new GetCurrencyByIdQuery(id));
        return HandleResult(result);
    }

    // POST: api/Currencies
    [HttpPost]
    public async Task<IActionResult> PostCurrency([FromBody] CreateCurrencyDto dto)
    {
        var command = new CreateCurrencyCommand(dto);
        var result = await Mediator.Send(command);
        return HandleResult(result);
    }

    // PUT: api/Currencies
    [HttpPut]
    public async Task<IActionResult> PutCurrency([FromBody] CurrencyDto dto)
    {
        var command = new UpdateCurrencyCommand(dto);
        var result = await Mediator.Send(command);
        return HandleResult(result);
    }

    // DELETE: api/Currencies/5
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteCurrency(int id)
    {
        var command = new DeleteCurrencyCommand(id);
        var result = await Mediator.Send(command);
        return HandleResult(result);
    }
}