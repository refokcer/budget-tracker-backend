using AutoMapper;
using budget_tracker_backend.Dto.Pages;
using budget_tracker_backend.Models;

namespace budget_tracker_backend.Mapping.Pages;

public class TransactionsFilteredProfile : Profile
{
    public TransactionsFilteredProfile()
    {
        CreateMap<Transaction, FilteredTxDto>()
            .ForMember(d => d.CurrencySymbol,
                m => m.MapFrom(s => s.Currency.Symbol))
            .ForMember(d => d.CategoryTitle,
                m => m.MapFrom(s => s.Category.Title))
            .ForMember(d => d.AccountFromTitle,
                m => m.MapFrom(s => s.FromAccount.Title))
            .ForMember(d => d.AccountToTitle,
                m => m.MapFrom(s => s.ToAccount.Title))
            .ForMember(d => d.BudetPlanTitle,
                m => m.MapFrom(s => s.BudgetPlan.Title));
    }
}
