using AutoMapper;
using budget_tracker_backend.Dto.Currencies;
using budget_tracker_backend.Models;

namespace budget_tracker_backend.Mapping;

public class CurrencyProfile : Profile
{
    public CurrencyProfile()
    {
        // Сущность → Dto
        CreateMap<Currency, CurrencyDto>();

        // DTO создания → сущность
        CreateMap<CreateCurrencyDto, Currency>();

        // Dto → сущность (для Update)
        CreateMap<CurrencyDto, Currency>();
    }
}