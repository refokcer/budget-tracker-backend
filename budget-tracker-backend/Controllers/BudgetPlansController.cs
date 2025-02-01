using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using budget_tracker_backend.Data;
using budget_tracker_backend.Models;

namespace budget_tracker_backend.Controllers
{
    public class BudgetPlansController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BudgetPlansController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: BudgetPlans
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.BudgetPlans.Include(b => b.Category);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: BudgetPlans/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var budgetPlan = await _context.BudgetPlans
                .Include(b => b.Category)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (budgetPlan == null)
            {
                return NotFound();
            }

            return View(budgetPlan);
        }

        // GET: BudgetPlans/Create
        public IActionResult Create()
        {
            ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Id");
            return View();
        }

        // POST: BudgetPlans/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,CategoryId,Amount,CurrencyId,StartDate,EndDate,Type,Description")] BudgetPlan budgetPlan)
        {
            if (ModelState.IsValid)
            {
                _context.Add(budgetPlan);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Id", budgetPlan.CategoryId);
            return View(budgetPlan);
        }

        // GET: BudgetPlans/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var budgetPlan = await _context.BudgetPlans.FindAsync(id);
            if (budgetPlan == null)
            {
                return NotFound();
            }
            ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Id", budgetPlan.CategoryId);
            return View(budgetPlan);
        }

        // POST: BudgetPlans/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,CategoryId,Amount,CurrencyId,StartDate,EndDate,Type,Description")] BudgetPlan budgetPlan)
        {
            if (id != budgetPlan.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(budgetPlan);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!BudgetPlanExists(budgetPlan.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Id", budgetPlan.CategoryId);
            return View(budgetPlan);
        }

        // GET: BudgetPlans/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var budgetPlan = await _context.BudgetPlans
                .Include(b => b.Category)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (budgetPlan == null)
            {
                return NotFound();
            }

            return View(budgetPlan);
        }

        // POST: BudgetPlans/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var budgetPlan = await _context.BudgetPlans.FindAsync(id);
            if (budgetPlan != null)
            {
                _context.BudgetPlans.Remove(budgetPlan);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool BudgetPlanExists(int id)
        {
            return _context.BudgetPlans.Any(e => e.Id == id);
        }
    }
}
