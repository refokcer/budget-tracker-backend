using AutoMapper;
using budget_tracker_backend.Dto.BudgetPlanItems;
using budget_tracker_backend.Models;

namespace budget_tracker_backend.Mapping;

public class BudgetPlanItemProfile : Profile
{
    public BudgetPlanItemProfile()
    {
        // Мапим сущность -> Dto
        CreateMap<BudgetPlanItem, BudgetPlanItemDto>();

        // Мапим CreateDto -> сущность
        CreateMap<CreateBudgetPlanItemDto, BudgetPlanItem>();

        // Мапим Dto -> сущность (для Update)
        CreateMap<BudgetPlanItemDto, BudgetPlanItem>();
    }
}
