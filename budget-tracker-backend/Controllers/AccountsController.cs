using budget_tracker_backend.Dto.Accounts;
using budget_tracker_backend.MediatR.Accounts.Commands.Create;
using budget_tracker_backend.MediatR.Accounts.Commands.Delete;
using budget_tracker_backend.MediatR.Accounts.Commands.Update;
using budget_tracker_backend.MediatR.Accounts.Queries.GetAll;
using budget_tracker_backend.MediatR.Accounts.Queries.GetById;
using Microsoft.AspNetCore.Mvc;

namespace budget_tracker_backend.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AccountsController : BaseApiController
{
    // GET: api/Accounts
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await Mediator.Send(new GetAllAccountsQuery());
        return HandleResult(result);
    }

    // GET: api/Accounts/5
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById([FromRoute] int id)
    {
        var result = await Mediator.Send(new GetAccountByIdQuery(id));
        return HandleResult(result);
    }

    // POST: api/Accounts
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateAccountDto accountDto)
    {
        var command = new CreateAccountCommand(accountDto);
        var result = await Mediator.Send(command);
        return HandleResult(result);
    }

    // PUT: api/Accounts
    [HttpPut]
    public async Task<IActionResult> Update([FromBody] AccountDto accountDto)
    {
        var command = new UpdateAccountCommand(accountDto);
        var result = await Mediator.Send(command);
        return HandleResult(result);
    }

    // DELETE: api/Accounts/5
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var command = new DeleteAccountCommand(id);
        var result = await Mediator.Send(command);
        return HandleResult(result);
    }
}