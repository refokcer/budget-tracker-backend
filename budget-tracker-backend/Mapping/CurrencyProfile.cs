using AutoMapper;
using budget_tracker_backend.Dto.Currencies;
using budget_tracker_backend.Models;

namespace budget_tracker_backend.Mapping;

public class CurrencyProfile : Profile
{
    public CurrencyProfile()
    {
        CreateMap<Currency, CurrencyDto>()
            .ForMember(d => d.Symbol, o => o.MapFrom(s => s.Symbol.ToString()));

        CreateMap<CreateCurrencyDto, Currency>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.Symbol, o => o.MapFrom(s => string.IsNullOrEmpty(s.Symbol) ? default(char) : s.Symbol[0]));

        CreateMap<CurrencyDto, Currency>()
            .ForMember(d => d.Symbol, o => o.MapFrom(s => string.IsNullOrEmpty(s.Symbol) ? default(char) : s.Symbol[0]));
    }
}
