using AutoMapper;
using FluentResults;
using MediatR;
using Microsoft.EntityFrameworkCore;
using budget_tracker_backend.Data;
using budget_tracker_backend.Dto.Pages;
using budget_tracker_backend.Models.Enums;

namespace budget_tracker_backend.MediatR.Pages.ExpensesByMonth;

public class GetExpensesByMonthHandler
    : IRequestHandler<GetExpensesByMonthQuery, Result<ExpensesByMonthDto>>
{
    private readonly ApplicationDbContext _ctx;
    private readonly IMapper _mapper;

    public GetExpensesByMonthHandler(ApplicationDbContext ctx, IMapper mapper)
    {
        _ctx = ctx; _mapper = mapper;
    }

    public async Task<Result<ExpensesByMonthDto>> Handle(
        GetExpensesByMonthQuery rq, CancellationToken ct)
    {
        if (rq.Month is < 1 or > 12)
            return Result.Fail("Month must be 1-12");

        var year = rq.Year ?? DateTime.Today.Year;
        var start = new DateTime(year, rq.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = start.AddMonths(1);

        var tx = await _ctx.Transactions
            .Include(t => t.Currency)
            .Include(t => t.Category)
            .Include(t => t.FromAccount)
            .Where(t =>
                   t.Type == TransactionCategoryType.Expense &&
                   t.Date >= start && t.Date < end)
            .AsNoTracking()
            .ToListAsync(ct);

        var dto = new ExpensesByMonthDto
        {
            Start = start,
            End = end,
            Transactions = _mapper.Map<List<ExpenseTxDto>>(tx)
        };

        return Result.Ok(dto);
    }
}
