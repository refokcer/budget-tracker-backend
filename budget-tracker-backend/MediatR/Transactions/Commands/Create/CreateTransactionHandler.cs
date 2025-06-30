using AutoMapper;
using budget_tracker_backend.Services.Transactions;
using budget_tracker_backend.Dto.Transactions;
using budget_tracker_backend.MediatR.Transactions.Commands.Create;
using budget_tracker_backend.Models;
using budget_tracker_backend.Models.Enums;
using FluentResults;
using MediatR;

public class CreateTransactionHandler : IRequestHandler<CreateTransactionCommand, Result<TransactionDto>>
{
    private readonly ITransactionManager _manager;
    private readonly IMapper _mapper;

    public CreateTransactionHandler(ITransactionManager manager, IMapper mapper)
    {
        _manager = manager;
        _mapper = mapper;
    }

    public async Task<Result<TransactionDto>> Handle(CreateTransactionCommand request, CancellationToken token)
    {
        var entity = await _manager.CreateAsync(request.NewTransaction, token);
        var resultDto = _mapper.Map<TransactionDto>(entity);
        return Result.Ok(resultDto);
    }
}
