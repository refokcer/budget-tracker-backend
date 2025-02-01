﻿using System;
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
    public class TransactionsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TransactionsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Transactions
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.Transactions.Include(t => t.Category).Include(t => t.Currency).Include(t => t.Event).Include(t => t.FromAccount).Include(t => t.ToAccount);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: Transactions/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var transaction = await _context.Transactions
                .Include(t => t.Category)
                .Include(t => t.Currency)
                .Include(t => t.Event)
                .Include(t => t.FromAccount)
                .Include(t => t.ToAccount)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (transaction == null)
            {
                return NotFound();
            }

            return View(transaction);
        }

        // GET: Transactions/Create
        public IActionResult Create()
        {
            ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Id");
            ViewData["CurrencyId"] = new SelectList(_context.Currencies, "Id", "Id");
            ViewData["EventId"] = new SelectList(_context.Events, "Id", "Id");
            ViewData["AccountFrom"] = new SelectList(_context.Accounts, "Id", "Id");
            ViewData["AccountTo"] = new SelectList(_context.Accounts, "Id", "Id");
            return View();
        }

        // POST: Transactions/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Title,Amount,EventId,CurrencyId,CategoryId,Date,AccountFrom,AccountTo,Type,Description")] Transaction transaction)
        {
            if (ModelState.IsValid)
            {
                _context.Add(transaction);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Id", transaction.CategoryId);
            ViewData["CurrencyId"] = new SelectList(_context.Currencies, "Id", "Id", transaction.CurrencyId);
            ViewData["EventId"] = new SelectList(_context.Events, "Id", "Id", transaction.EventId);
            ViewData["AccountFrom"] = new SelectList(_context.Accounts, "Id", "Id", transaction.AccountFrom);
            ViewData["AccountTo"] = new SelectList(_context.Accounts, "Id", "Id", transaction.AccountTo);
            return View(transaction);
        }

        // GET: Transactions/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var transaction = await _context.Transactions.FindAsync(id);
            if (transaction == null)
            {
                return NotFound();
            }
            ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Id", transaction.CategoryId);
            ViewData["CurrencyId"] = new SelectList(_context.Currencies, "Id", "Id", transaction.CurrencyId);
            ViewData["EventId"] = new SelectList(_context.Events, "Id", "Id", transaction.EventId);
            ViewData["AccountFrom"] = new SelectList(_context.Accounts, "Id", "Id", transaction.AccountFrom);
            ViewData["AccountTo"] = new SelectList(_context.Accounts, "Id", "Id", transaction.AccountTo);
            return View(transaction);
        }

        // POST: Transactions/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Amount,EventId,CurrencyId,CategoryId,Date,AccountFrom,AccountTo,Type,Description")] Transaction transaction)
        {
            if (id != transaction.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(transaction);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TransactionExists(transaction.Id))
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
            ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Id", transaction.CategoryId);
            ViewData["CurrencyId"] = new SelectList(_context.Currencies, "Id", "Id", transaction.CurrencyId);
            ViewData["EventId"] = new SelectList(_context.Events, "Id", "Id", transaction.EventId);
            ViewData["AccountFrom"] = new SelectList(_context.Accounts, "Id", "Id", transaction.AccountFrom);
            ViewData["AccountTo"] = new SelectList(_context.Accounts, "Id", "Id", transaction.AccountTo);
            return View(transaction);
        }

        // GET: Transactions/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var transaction = await _context.Transactions
                .Include(t => t.Category)
                .Include(t => t.Currency)
                .Include(t => t.Event)
                .Include(t => t.FromAccount)
                .Include(t => t.ToAccount)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (transaction == null)
            {
                return NotFound();
            }

            return View(transaction);
        }

        // POST: Transactions/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var transaction = await _context.Transactions.FindAsync(id);
            if (transaction != null)
            {
                _context.Transactions.Remove(transaction);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool TransactionExists(int id)
        {
            return _context.Transactions.Any(e => e.Id == id);
        }
    }
}
