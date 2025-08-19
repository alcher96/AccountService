using AccountService.Features.Transactions.AddTransaction.Command;
using AccountService.Repositories;
using AutoMapper;
using FluentValidation;
using MediatR;
#pragma warning disable CS8613 // Nullability of reference types in return type doesn't match implicitly implemented member.
#pragma warning disable CS1591 // Избыточный xml комментарий

namespace AccountService.Features.Transactions.AddTransaction
{
    // ReSharper disable once UnusedMember.Global
    public class CreateTransactionCommandHandler(
        IAccountRepository accountRepository,
        IMapper mapper,
        IValidator<CreateTransactionCommand> validator)
        : IRequestHandler<CreateTransactionCommand, MbResult<TransactionDto>>
    {
        public async Task<MbResult<TransactionDto>> Handle(CreateTransactionCommand request, CancellationToken cancellationToken)
        {
            var validationResult = await validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
                return MbResult<TransactionDto>.Failure(errors);
            }

            var transaction = mapper.Map<Transaction>(request);


         
                var result = await accountRepository.AddTransactionAsync(request.AccountId, transaction);
                
                var transactionDto = mapper.Map<TransactionDto>(result);
                return MbResult<TransactionDto>.Success(transactionDto);
          
        }
    }
}
