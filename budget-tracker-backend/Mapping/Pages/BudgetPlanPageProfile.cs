using AutoMapper;
using budget_tracker_backend.Dto.Pages;
using budget_tracker_backend.Models;

namespace budget_tracker_backend.Mapping.Pages;

public class BudgetPlanPageProfile : Profile
{
    public BudgetPlanPageProfile()
    {
        CreateMap<BudgetPlanItem, BudgetPlanPageItemDto>()
            .ForMember(d => d.CategoryTitle,
                m => m.MapFrom(s => s.Category.Title))
            .ForMember(d => d.CurrencySymbol,
                m => m.MapFrom(s => s.Currency.Symbol))
            .ForMember(d => d.Spent, m => m.Ignore())
            .ForMember(d => d.Remaining, m => m.Ignore())
            .ForMember(d => d.ProjectedSpent, m => m.Ignore())
            .ForMember(d => d.ProjectedRemaining, m => m.Ignore())
            .ForMember(d => d.ProjectedVariableSpending, m => m.Ignore())
            .ForMember(d => d.FutureRecurringSpending, m => m.Ignore())
            .ForMember(d => d.IsOther, m => m.Ignore())
            .ForMember(d => d.IsVirtual, m => m.Ignore())
            .ForMember(d => d.IsEventSummary, m => m.Ignore());
    }
}
