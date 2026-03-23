using AutoMapper;
using budget_tracker_backend.Dto.Accounts;
using budget_tracker_backend.Models;

namespace budget_tracker_backend.Mapping;

public class AccountProfile : Profile
{
    public AccountProfile()
    {
        CreateMap<Account, AccountDto>();

        CreateMap<CreateAccountDto, Account>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.UserId, o => o.Ignore())
            .ForMember(d => d.Currency, o => o.Ignore())
            .ForMember(d => d.User, o => o.Ignore());
    }
}
