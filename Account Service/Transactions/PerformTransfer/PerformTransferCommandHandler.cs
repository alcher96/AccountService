using Account_Service.Repositories;
using Account_Service.Transactions.PerformTransfer.Command;
using AutoMapper;
using MediatR;

namespace Account_Service.Transactions.PerformTransfer
{
    public class PerformTransferCommandHandler(IAccountRepository accountRepository, IMapper mapper)
        : IRequestHandler<PerformTransferCommand, TransactionDto[]>
    {
        public async Task<TransactionDto[]> Handle(PerformTransferCommand request, CancellationToken cancellationToken)
        {
            var fromAccount = await accountRepository.GetByIdAsync(request.FromAccountId);
            var toAccount = await accountRepository.GetByIdAsync(request.ToAccountId);
            var debitTransaction = mapper.Map<Transaction>(request, opt => opt.Items["TransactionType"] = TransactionType.Debit);
            var creditTransaction = mapper.Map<Transaction>(request, opt => opt.Items["TransactionType"] = TransactionType.Credit);

            fromAccount.Balance -= request.Amount;
            fromAccount.Transactions.Add(debitTransaction);
            toAccount.Balance += request.Amount;
            toAccount.Transactions.Add(creditTransaction);

            await accountRepository.UpdateAsync(fromAccount);
            await accountRepository.UpdateAsync(toAccount);

            return new[] { mapper.Map<TransactionDto>(debitTransaction), mapper.Map<TransactionDto>(creditTransaction) };
        }
    }
}
