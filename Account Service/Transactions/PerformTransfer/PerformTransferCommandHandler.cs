using Account_Service.Repositories;
using Account_Service.Transactions.PerformTransfer.Command;
using AutoMapper;
using FluentValidation;
using MediatR;
#pragma warning disable CS8613 // Nullability of reference types in return type doesn't match implicitly implemented member.
#pragma warning disable CS1591 // Избыточный xml комментарий

namespace Account_Service.Transactions.PerformTransfer
{
    // ReSharper disable once UnusedMember.Global
    public class PerformTransferCommandHandler(
        IAccountRepository accountRepository,
        IMapper mapper,
        IValidator<PerformTransferCommand> validator)
        : IRequestHandler<PerformTransferCommand, MbResult<TransactionDto[]>>
    {
        public async Task<MbResult<TransactionDto[]?>> Handle(PerformTransferCommand request,
            CancellationToken cancellationToken)
        {
            // Валидация команды
            var validationResult = await validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
                return MbResult<TransactionDto[]>.Failure(errors)!;
            }

            var fromAccount = await accountRepository.GetByIdAsync(request.FromAccountId);
            var toAccount = await accountRepository.GetByIdAsync(request.ToAccountId);


            if (fromAccount.Balance < request.Amount)
            {
                return MbResult<TransactionDto[]>.Failure("Недостаточно средств на счёте отправителя")!;
            }


            var debitTransaction =
                mapper.Map<Transaction>(request, opt => opt.Items["TransactionType"] = TransactionType.Debit);
            var creditTransaction =
                mapper.Map<Transaction>(request, opt => opt.Items["TransactionType"] = TransactionType.Credit);

 
            fromAccount.Balance -= request.Amount;
            fromAccount.Transactions.Add(creditTransaction); // Credit = списание
            toAccount.Balance += request.Amount;
            toAccount.Transactions.Add(debitTransaction); // Debit = зачисление

            await accountRepository.UpdateAsync(fromAccount);
            await accountRepository.UpdateAsync(toAccount);

            var result = new[]
            {
                mapper.Map<TransactionDto>(creditTransaction),
                mapper.Map<TransactionDto>(debitTransaction)
            };
            return MbResult<TransactionDto[]>.Success(result)!;
        }
    }
}
