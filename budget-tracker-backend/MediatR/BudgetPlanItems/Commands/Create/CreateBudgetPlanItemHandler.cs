﻿using AutoMapper;
using FluentResults;
using MediatR;
using budget_tracker_backend.Data;
using budget_tracker_backend.Models;
using budget_tracker_backend.Dto.BudgetPlanItems;

namespace budget_tracker_backend.MediatR.BudgetPlanItems.Commands.Create;

public class CreateBudgetPlanItemHandler : IRequestHandler<CreateBudgetPlanItemCommand, Result<BudgetPlanItemDto>>
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;

    public CreateBudgetPlanItemHandler(ApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<Result<BudgetPlanItemDto>> Handle(CreateBudgetPlanItemCommand request, CancellationToken cancellationToken)
    {
        var entity = _mapper.Map<BudgetPlanItem>(request.ItemDto);
        if (entity == null)
        {
            return Result.Fail("Cannot map CreateBudgetPlanItemDto to BudgetPlanItem");
        }

        // Доп. проверка, что BudgetPlanId существует, если нужно:
        // var plan = await _context.BudgetPlans.FindAsync(entity.BudgetPlanId);
        // if (plan == null) {
        //     return Result.Fail($"BudgetPlan with Id={entity.BudgetPlanId} not found");
        // }

        _context.BudgetPlanItems.Add(entity);
        var saved = await _context.SaveChangesAsync(cancellationToken) > 0;

        if (!saved)
        {
            return Result.Fail("Failed to create BudgetPlanItem");
        }

        // Мапим обратно в Dto
        var itemDto = _mapper.Map<BudgetPlanItemDto>(entity);
        return Result.Ok(itemDto);
    }
}
