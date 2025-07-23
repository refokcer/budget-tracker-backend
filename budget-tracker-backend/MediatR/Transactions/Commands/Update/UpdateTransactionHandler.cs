using AutoMapper;
using FluentResults;
using MediatR;
using budget_tracker_backend.Dto.Transactions;
using budget_tracker_backend.Services.Transactions;

namespace budget_tracker_backend.MediatR.Transactions.Commands.Update;

public class UpdateTransactionHandler
    : IRequestHandler<UpdateTransactionCommand, Result<TransactionDto>>
{
    private readonly ITransactionManager _manager;
    private readonly IMapper _mapper;

    public UpdateTransactionHandler(ITransactionManager manager, IMapper mapper)
    {
        _manager = manager;
        _mapper = mapper;
    }

    public async Task<Result<TransactionDto>> Handle(UpdateTransactionCommand request, CancellationToken ct)
    {
        var entity = await _manager.UpdateAsync(request.TransactionToUpdate, ct);
        return Result.Ok(_mapper.Map<TransactionDto>(entity));
    }
}
