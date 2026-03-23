using AutoMapper;
using budget_tracker_backend.Dto.BudgetPlans;
using budget_tracker_backend.Models;

namespace budget_tracker_backend.Mapping;

public class BudgetPlanProfile : Profile
{
    public BudgetPlanProfile()
    {
        CreateMap<BudgetPlan, BudgetPlanDto>();

        CreateMap<CreateBudgetPlanDto, BudgetPlan>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.UserId, o => o.Ignore())
            .ForMember(d => d.Items, o => o.Ignore())
            .ForMember(d => d.User, o => o.Ignore())
            .ForMember(d => d.Parent, o => o.Ignore());

        CreateMap<BudgetPlanDto, BudgetPlan>()
            .ForMember(d => d.UserId, o => o.Ignore())
            .ForMember(d => d.Items, o => o.Ignore())
            .ForMember(d => d.User, o => o.Ignore())
            .ForMember(d => d.Parent, o => o.Ignore());
    }
}
