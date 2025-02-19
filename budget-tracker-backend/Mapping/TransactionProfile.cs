using AutoMapper;
using budget_tracker_backend.Dto.Transactions;
using budget_tracker_backend.Models;

namespace budget_tracker_backend.Mapping;

public class TransactionProfile : Profile
{
    public TransactionProfile()
    {
        // Сущность → DTO
        CreateMap<Transaction, TransactionDto>();

        // DTO создания → сущность
        CreateMap<CreateTransactionDto, Transaction>();

        // DTO (полный) → сущность (для Update)
        CreateMap<TransactionDto, Transaction>();
    }
}