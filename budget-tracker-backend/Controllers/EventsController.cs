using Microsoft.AspNetCore.Mvc;
using budget_tracker_backend.Dto.Events;
using budget_tracker_backend.MediatR.Events.Commands.Create;
using budget_tracker_backend.MediatR.Events.Commands.Delete;
using budget_tracker_backend.MediatR.Events.Commands.Update;
using budget_tracker_backend.MediatR.Events.Queries.GetAll;
using budget_tracker_backend.MediatR.Events.Queries.GetById;
using Streetcode.WebApi.Controllers;

namespace budget_tracker_backend.Controllers;

[Route("api/[controller]")]
[ApiController]
public class EventsController : BaseApiController
{
    // GET: api/Events
    [HttpGet]
    public async Task<IActionResult> GetEvents()
    {
        var result = await Mediator.Send(new GetAllEventsQuery());
        return HandleResult(result);
    }

    // GET: api/Events/5
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetEvent(int id)
    {
        var result = await Mediator.Send(new GetEventByIdQuery(id));
        return HandleResult(result);
    }

    // POST: api/Events
    [HttpPost]
    public async Task<IActionResult> PostEvent([FromBody] CreateEventDto dto)
    {
        var command = new CreateEventCommand(dto);
        var result = await Mediator.Send(command);
        return HandleResult(result);
    }

    // PUT: api/Events
    [HttpPut]
    public async Task<IActionResult> PutEvent([FromBody] EventDto dto)
    {
        var command = new UpdateEventCommand(dto);
        var result = await Mediator.Send(command);
        return HandleResult(result);
    }

    // DELETE: api/Events/5
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteEvent(int id)
    {
        var command = new DeleteEventCommand(id);
        var result = await Mediator.Send(command);
        return HandleResult(result);
    }
}
