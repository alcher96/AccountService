using Account_Service.Repositories;
using Account_Service.Transactions.AddTransaction.Command;
using AutoMapper;
using MediatR;

namespace Account_Service.Transactions.AddTransaction
{
    public class CreateTransactionCommandHandler(IAccountRepository accountRepository, IMapper mapper)
        : IRequestHandler<CreateTransactionCommand, TransactionDto>
    {
        public async Task<TransactionDto> Handle(CreateTransactionCommand request, CancellationToken cancellationToken)
        {
            var account = await accountRepository.GetByIdAsync(request.AccountId);
            var transaction = mapper.Map<Transaction>(request);
            account.Transactions.Add(transaction);
            account.Balance += request.Type == TransactionType.Credit ? request.Amount : -request.Amount;
            await accountRepository.UpdateAsync(account);
            return mapper.Map<TransactionDto>(transaction);
        }
    }
}
