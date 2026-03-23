using AutoMapper;
using budget_tracker_backend.Dto.BudgetPlanItems;
using budget_tracker_backend.Models;

namespace budget_tracker_backend.Mapping;

public class BudgetPlanItemProfile : Profile
{
    public BudgetPlanItemProfile()
    {
        CreateMap<BudgetPlanItem, BudgetPlanItemDto>();

        CreateMap<CreateBudgetPlanItemDto, BudgetPlanItem>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.BudgetPlan, o => o.Ignore())
            .ForMember(d => d.Category, o => o.Ignore())
            .ForMember(d => d.Currency, o => o.Ignore());

        CreateMap<BudgetPlanItemDto, BudgetPlanItem>()
            .ForMember(d => d.BudgetPlan, o => o.Ignore())
            .ForMember(d => d.Category, o => o.Ignore())
            .ForMember(d => d.Currency, o => o.Ignore());
    }
}
