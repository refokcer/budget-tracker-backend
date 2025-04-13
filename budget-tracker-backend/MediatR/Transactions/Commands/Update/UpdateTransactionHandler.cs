using AutoMapper;
using FluentResults;
using MediatR;
using budget_tracker_backend.Data;
using budget_tracker_backend.Dto.Transactions;
using budget_tracker_backend.Models;

namespace budget_tracker_backend.MediatR.Transactions.Commands.Update;

public class UpdateTransactionHandler : IRequestHandler<UpdateTransactionCommand, Result<TransactionDto>>
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;

    public UpdateTransactionHandler(ApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<Result<TransactionDto>> Handle(UpdateTransactionCommand request, CancellationToken cancellationToken)
    {
        var dto = request.TransactionToUpdate;
        var existing = await _context.Transactions.FindAsync(new object[] { dto.Id }, cancellationToken);
        if (existing == null)
        {
            return Result.Fail($"Transaction with Id={dto.Id} not found");
        }

        _mapper.Map(dto, existing);

        _context.Transactions.Update(existing);
        var saved = await _context.SaveChangesAsync(cancellationToken) > 0;
        if (!saved)
        {
            return Result.Fail("Failed to update Transaction");
        }

        var updatedDto = _mapper.Map<TransactionDto>(existing);
        return Result.Ok(updatedDto);
    }
}