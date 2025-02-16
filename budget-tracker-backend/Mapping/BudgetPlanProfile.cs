using AutoMapper;
using budget_tracker_backend.Dto.BudgetPlans;
using budget_tracker_backend.Models;

namespace budget_tracker_backend.Mapping;

public class BudgetPlanProfile : Profile
{
    public BudgetPlanProfile()
    {
        // Мапим сущность → DTO
        CreateMap<BudgetPlan, BudgetPlanDto>();

        // Мапим CreateDTO → сущность
        CreateMap<CreateBudgetPlanDto, BudgetPlan>();

        // Дополнительно, если нужно, маппинг из BudgetPlanDto → BudgetPlan
        CreateMap<BudgetPlanDto, BudgetPlan>();
    }
}
