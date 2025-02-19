using AutoMapper;
using FluentResults;
using MediatR;
using budget_tracker_backend.Data;
using budget_tracker_backend.Dto.Transactions;
using budget_tracker_backend.Models;

namespace budget_tracker_backend.MediatR.Transactions.Commands.Create;

public class CreateTransactionHandler : IRequestHandler<CreateTransactionCommand, Result<TransactionDto>>
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;

    public CreateTransactionHandler(ApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<Result<TransactionDto>> Handle(CreateTransactionCommand request, CancellationToken cancellationToken)
    {
        var entity = _mapper.Map<Transaction>(request.NewTransaction);
        if (entity == null)
        {
            return Result.Fail("Cannot map CreateTransactionDto to Transaction");
        }

        _context.Transactions.Add(entity);
        var saved = await _context.SaveChangesAsync(cancellationToken) > 0;
        if (!saved)
        {
            return Result.Fail("Failed to create Transaction in database");
        }

        var resultDto = _mapper.Map<TransactionDto>(entity);
        return Result.Ok(resultDto);
    }
}