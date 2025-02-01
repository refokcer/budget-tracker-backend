using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using budget_tracker_backend.Data;
using budget_tracker_backend.Models;

namespace budget_tracker_backend.Controllers;

[Route("api/[controller]")]
[ApiController]
public class BudgetPlansController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public BudgetPlansController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: api/BudgetPlans
    [HttpGet]
    public async Task<ActionResult<IEnumerable<BudgetPlan>>> GetBudgetPlans()
    {
        return await _context.BudgetPlans.ToListAsync();
    }

    // GET: api/BudgetPlans/5
    [HttpGet("{id}")]
    public async Task<ActionResult<BudgetPlan>> GetBudgetPlan(int id)
    {
        var budgetPlan = await _context.BudgetPlans.FindAsync(id);

        if (budgetPlan == null)
        {
            return NotFound();
        }

        return budgetPlan;
    }

    // PUT: api/BudgetPlans/5
    // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
    [HttpPut("{id}")]
    public async Task<IActionResult> PutBudgetPlan(int id, BudgetPlan budgetPlan)
    {
        if (id != budgetPlan.Id)
        {
            return BadRequest();
        }

        _context.Entry(budgetPlan).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!BudgetPlanExists(id))
            {
                return NotFound();
            }
            else
            {
                throw;
            }
        }

        return NoContent();
    }

    // POST: api/BudgetPlans
    // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
    [HttpPost]
    public async Task<ActionResult<BudgetPlan>> PostBudgetPlan(BudgetPlan budgetPlan)
    {
        _context.BudgetPlans.Add(budgetPlan);
        await _context.SaveChangesAsync();

        return CreatedAtAction("GetBudgetPlan", new { id = budgetPlan.Id }, budgetPlan);
    }

    // DELETE: api/BudgetPlans/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteBudgetPlan(int id)
    {
        var budgetPlan = await _context.BudgetPlans.FindAsync(id);
        if (budgetPlan == null)
        {
            return NotFound();
        }

        _context.BudgetPlans.Remove(budgetPlan);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private bool BudgetPlanExists(int id)
    {
        return _context.BudgetPlans.Any(e => e.Id == id);
    }
}
