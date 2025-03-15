using AutoMapper;
using budget_tracker_backend.Dto.Events;
using budget_tracker_backend.Models;

namespace budget_tracker_backend.Mapping;

public class EventProfile : Profile
{
    public EventProfile()
    {
        // Из сущности -> Dto
        CreateMap<Event, EventDto>();

        // Из Dto создания -> сущность
        CreateMap<CreateEventDto, Event>();

        // Из полного EventDto -> сущность (для Update)
        CreateMap<EventDto, Event>();

        CreateMap<Event, EventWithTransactionsDto>()
            .ForMember(dst => dst.Transactions,
                       opt => opt.MapFrom(src => src.Transactions));
    }
}