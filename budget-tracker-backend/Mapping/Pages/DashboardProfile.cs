using AutoMapper;
using budget_tracker_backend.Dto.Pages;
using budget_tracker_backend.Models;

namespace budget_tracker_backend.Mapping.Pages;

public class DashboardProfile : Profile
{
    public DashboardProfile()
    {
        CreateMap<Account, DashboardAccountDto>()
            .ForMember(d => d.CurrencySymbol,
                m => m.MapFrom(s => s.Currency.Symbol));
    }
}
