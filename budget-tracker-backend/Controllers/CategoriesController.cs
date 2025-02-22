using Microsoft.AspNetCore.Mvc;
using budget_tracker_backend.Dto.Categories;
using budget_tracker_backend.MediatR.Categories.Commands.Create;
using budget_tracker_backend.MediatR.Categories.Commands.Delete;
using budget_tracker_backend.MediatR.Categories.Commands.Update;
using budget_tracker_backend.MediatR.Categories.Queries.GetAll;
using budget_tracker_backend.MediatR.Categories.Queries.GetById;

namespace budget_tracker_backend.Controllers;

[Route("api/[controller]")]
[ApiController]
public class CategoriesController : BaseApiController
{
    // GET: api/Categories
    [HttpGet]
    public async Task<IActionResult> GetCategories()
    {
        var result = await Mediator.Send(new GetAllCategoriesQuery());
        return HandleResult(result);
    }

    // GET: api/Categories/5
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetCategory(int id)
    {
        var result = await Mediator.Send(new GetCategoryByIdQuery(id));
        return HandleResult(result);
    }

    // POST: api/Categories
    [HttpPost]
    public async Task<IActionResult> PostCategory([FromBody] CreateCategoryDto dto)
    {
        var command = new CreateCategoryCommand(dto);
        var result = await Mediator.Send(command);
        return HandleResult(result);
    }

    // PUT: api/Categories
    [HttpPut]
    public async Task<IActionResult> PutCategory([FromBody] CategoryDto dto)
    {
        var command = new UpdateCategoryCommand(dto);
        var result = await Mediator.Send(command);
        return HandleResult(result);
    }

    // DELETE: api/Categories/5
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        var command = new DeleteCategoryCommand(id);
        var result = await Mediator.Send(command);
        return HandleResult(result);
    }
}
