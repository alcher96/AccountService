using Account_Service.Repositories;
using Account_Service.Transactions.GetAccountTransactions.Query;
using AutoMapper;
using MediatR;

namespace Account_Service.Transactions.GetAccountTransactions
{
    public class GetAccountTransactionsQueryHandler(IAccountRepository accountRepository, IMapper mapper)
        : IRequestHandler<GetAccountTransactionsQuery, List<TransactionDto>>
    {
        public async Task<List<TransactionDto>> Handle(GetAccountTransactionsQuery request, CancellationToken cancellationToken)
        {
            var account = await accountRepository.GetByIdAsync(request.AccountId);
            var transactions = account.Transactions.AsQueryable();
            if (request.StartDate.HasValue)
                transactions = transactions.Where(t => t.DateTime >= request.StartDate.Value);
            if (request.EndDate.HasValue)
                transactions = transactions.Where(t => t.DateTime <= request.EndDate.Value);

            return mapper.Map<List<TransactionDto>>(transactions.ToList());
        }
    }
}
