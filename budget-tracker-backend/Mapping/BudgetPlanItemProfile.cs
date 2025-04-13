using AutoMapper;
using budget_tracker_backend.Dto.BudgetPlanItems;
using budget_tracker_backend.Models;

namespace budget_tracker_backend.Mapping;

public class BudgetPlanItemProfile : Profile
{
    public BudgetPlanItemProfile()
    {
        CreateMap<BudgetPlanItem, BudgetPlanItemDto>();

        CreateMap<CreateBudgetPlanItemDto, BudgetPlanItem>();

        CreateMap<BudgetPlanItemDto, BudgetPlanItem>();
    }
}
