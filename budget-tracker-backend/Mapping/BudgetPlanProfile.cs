using AutoMapper;
using budget_tracker_backend.Dto.BudgetPlans;
using budget_tracker_backend.Models;

namespace budget_tracker_backend.Mapping;

public class BudgetPlanProfile : Profile
{
    public BudgetPlanProfile()
    {
        CreateMap<BudgetPlan, BudgetPlanDto>();

        CreateMap<CreateBudgetPlanDto, BudgetPlan>();

        CreateMap<BudgetPlanDto, BudgetPlan>();
    }
}
