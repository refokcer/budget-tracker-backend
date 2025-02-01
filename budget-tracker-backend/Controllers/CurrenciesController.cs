﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using budget_tracker_backend.Data;
using budget_tracker_backend.Models;

namespace budget_tracker_backend.Controllers;

public class CurrenciesController : Controller
{
    private readonly ApplicationDbContext _context;

    public CurrenciesController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: Currencies
    public async Task<IActionResult> Index()
    {
        return View(await _context.Currencies.ToListAsync());
    }

    // GET: Currencies/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var currency = await _context.Currencies
            .FirstOrDefaultAsync(m => m.Id == id);
        if (currency == null)
        {
            return NotFound();
        }

        return View(currency);
    }

    // GET: Currencies/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: Currencies/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Id,Title,Code,Symbol,IsBase")] Currency currency)
    {
        if (ModelState.IsValid)
        {
            _context.Add(currency);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(currency);
    }

    // GET: Currencies/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var currency = await _context.Currencies.FindAsync(id);
        if (currency == null)
        {
            return NotFound();
        }
        return View(currency);
    }

    // POST: Currencies/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Code,Symbol,IsBase")] Currency currency)
    {
        if (id != currency.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(currency);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CurrencyExists(currency.Id))
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
        return View(currency);
    }

    // GET: Currencies/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var currency = await _context.Currencies
            .FirstOrDefaultAsync(m => m.Id == id);
        if (currency == null)
        {
            return NotFound();
        }

        return View(currency);
    }

    // POST: Currencies/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var currency = await _context.Currencies.FindAsync(id);
        if (currency != null)
        {
            _context.Currencies.Remove(currency);
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private bool CurrencyExists(int id)
    {
        return _context.Currencies.Any(e => e.Id == id);
    }
}
