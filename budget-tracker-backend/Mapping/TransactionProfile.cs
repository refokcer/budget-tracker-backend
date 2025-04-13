using AutoMapper;
using budget_tracker_backend.Dto.Transactions;
using budget_tracker_backend.Models;

namespace budget_tracker_backend.Mapping;

public class TransactionProfile : Profile
{
    public TransactionProfile()
    {
        CreateMap<Transaction, TransactionDto>();

        CreateMap<CreateTransactionDto, Transaction>();

        CreateMap<TransactionDto, Transaction>();
    }
}