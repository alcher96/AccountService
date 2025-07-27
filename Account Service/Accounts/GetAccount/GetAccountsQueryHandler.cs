using Account_Service.Accounts.GetAccount.Query;
using Account_Service.Repositories;
using AutoMapper;
using MediatR;

namespace Account_Service.Accounts.GetAccount
{
    public class GetAccountsQueryHandler(IMapper mapper, IAccountRepository accountRepository)
        : IRequestHandler<GetAccountsQuery, List<AccountDto>>
    {
        public async Task<List<AccountDto>> Handle(GetAccountsQuery request, CancellationToken cancellationToken)
        {
            var accounts = await accountRepository.GetAllAsync(request.OwnerId, request.Type);
            return mapper.Map<List<AccountDto>>(accounts);
        }
    }
}
