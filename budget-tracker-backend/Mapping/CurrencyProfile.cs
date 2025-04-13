using AutoMapper;
using budget_tracker_backend.Dto.Currencies;
using budget_tracker_backend.Models;

namespace budget_tracker_backend.Mapping;

public class CurrencyProfile : Profile
{
    public CurrencyProfile()
    {
        CreateMap<Currency, CurrencyDto>();

        CreateMap<CreateCurrencyDto, Currency>();

        CreateMap<CurrencyDto, Currency>();
    }
}