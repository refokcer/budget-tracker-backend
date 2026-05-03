using AutoMapper;
using budget_tracker_backend.Dto.FinancialGoals;
using budget_tracker_backend.Models;

namespace budget_tracker_backend.Mapping;

public class FinancialGoalProfile : Profile
{
    public FinancialGoalProfile()
    {
        CreateMap<FinancialGoal, FinancialGoalDto>();

        CreateMap<CreateFinancialGoalDto, FinancialGoal>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.CreatedAt, o => o.Ignore())
            .ForMember(d => d.UserId, o => o.Ignore())
            .ForMember(d => d.LinkedAccount, o => o.Ignore())
            .ForMember(d => d.User, o => o.Ignore());

        CreateMap<FinancialGoalDto, FinancialGoal>()
            .ForMember(d => d.UserId, o => o.Ignore())
            .ForMember(d => d.LinkedAccount, o => o.Ignore())
            .ForMember(d => d.User, o => o.Ignore());
    }
}
