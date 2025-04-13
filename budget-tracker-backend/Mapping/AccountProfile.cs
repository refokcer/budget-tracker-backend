using AutoMapper;
using budget_tracker_backend.Dto.Accounts;
using budget_tracker_backend.Models;

namespace budget_tracker_backend.Mapping;

public class AccountProfile : Profile
{
    public AccountProfile()
    {
        CreateMap<Account, AccountDto>();

        CreateMap<CreateAccountDto, Account>();
    }
}
