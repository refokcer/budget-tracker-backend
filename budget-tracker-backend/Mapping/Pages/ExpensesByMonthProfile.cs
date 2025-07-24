using AutoMapper;
using budget_tracker_backend.Dto.Pages;
using budget_tracker_backend.Models;

namespace budget_tracker_backend.Mapping.Pages;

public class ExpensesByMonthProfile : Profile
{
    public ExpensesByMonthProfile()
    {
        CreateMap<Transaction, ExpenseTxDto>()
            .ForMember(d => d.CurrencySymbol,
                m => m.MapFrom(s => s.Currency.Symbol))
            .ForMember(d => d.CategoryTitle,
                m => m.MapFrom(s => s.Category.Title))
            .ForMember(d => d.AccountTitle,
                m => m.MapFrom(s => s.FromAccount.Title))
            .ForMember(d => d.BudetPlanTitle,
                m => m.MapFrom(s => s.BudgetPlan.Title));
            
    }
}
