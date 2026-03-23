using AutoMapper;
using budget_tracker_backend.Dto.Transactions;
using budget_tracker_backend.Models;

namespace budget_tracker_backend.Mapping;

public class TransactionProfile : Profile
{
    public TransactionProfile()
    {
        CreateMap<Transaction, TransactionDto>();

        CreateMap<CreateTransactionDto, Transaction>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.UnicCode, o => o.Ignore())
            .ForMember(d => d.UserId, o => o.Ignore())
            .ForMember(d => d.BudgetPlan, o => o.Ignore())
            .ForMember(d => d.Category, o => o.Ignore())
            .ForMember(d => d.Currency, o => o.Ignore())
            .ForMember(d => d.FromAccount, o => o.Ignore())
            .ForMember(d => d.ToAccount, o => o.Ignore())
            .ForMember(d => d.User, o => o.Ignore());

        CreateMap<TransactionDto, Transaction>()
            .ForMember(d => d.UnicCode, o => o.Ignore())
            .ForMember(d => d.UserId, o => o.Ignore())
            .ForMember(d => d.BudgetPlan, o => o.Ignore())
            .ForMember(d => d.Category, o => o.Ignore())
            .ForMember(d => d.Currency, o => o.Ignore())
            .ForMember(d => d.FromAccount, o => o.Ignore())
            .ForMember(d => d.ToAccount, o => o.Ignore())
            .ForMember(d => d.User, o => o.Ignore());

        CreateMap<UpdateTransactionDto, Transaction>()
            .ForMember(d => d.UnicCode, o => o.Ignore())
            .ForMember(d => d.UserId, o => o.Ignore())
            .ForMember(d => d.BudgetPlan, o => o.Ignore())
            .ForMember(d => d.Category, o => o.Ignore())
            .ForMember(d => d.Currency, o => o.Ignore())
            .ForMember(d => d.FromAccount, o => o.Ignore())
            .ForMember(d => d.ToAccount, o => o.Ignore())
            .ForMember(d => d.User, o => o.Ignore())
            .ForAllMembers(opts => opts.Condition((src, _, srcMember) => srcMember != null));
    }
}
