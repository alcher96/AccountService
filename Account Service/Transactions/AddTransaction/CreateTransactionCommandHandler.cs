using Account_Service.Repositories;
using Account_Service.Transactions.AddTransaction.Command;
using AutoMapper;
using FluentValidation;
using MediatR;
#pragma warning disable CS8613 // Nullability of reference types in return type doesn't match implicitly implemented member.
#pragma warning disable CS1591 // Избыточный xml комментарий

namespace Account_Service.Transactions.AddTransaction
{
    // ReSharper disable once UnusedMember.Global
    public class CreateTransactionCommandHandler(
        IAccountRepository accountRepository,
        IMapper mapper,
        IValidator<CreateTransactionCommand> validator)
        : IRequestHandler<CreateTransactionCommand, MbResult<TransactionDto>>
    {
        public async Task<MbResult<TransactionDto?>> Handle(CreateTransactionCommand request,
            CancellationToken cancellationToken)
        {
            // Валидация команды
            var validationResult = await validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
                return MbResult<TransactionDto>.Failure(errors)!;
            }

            var account = await accountRepository.GetByIdAsync(request.AccountId);

            if (request.Type == TransactionType.Credit && account.Balance < request.Amount)
            {
                return MbResult<TransactionDto>.Failure("Недостаточно средств на счёте")!;
            }

            
            var transaction = mapper.Map<Transaction>(request);
            account.Transactions.Add(transaction);
            account.Balance += request.Type == TransactionType.Debit ? request.Amount : -request.Amount;
            await accountRepository.UpdateAsync(account);
            var transactionDto = mapper.Map<TransactionDto>(transaction);
            return MbResult<TransactionDto>.Success(transactionDto)!;
        }
    }
}
