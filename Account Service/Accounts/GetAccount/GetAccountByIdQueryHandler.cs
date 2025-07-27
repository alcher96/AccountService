using Account_Service.Accounts.GetAccount.Query;
using Account_Service.Repositories;
using AutoMapper;
using MediatR;

namespace Account_Service.Accounts.GetAccount
{
    public class GetAccountByIdQueryHandler(IAccountRepository accountRepository, IMapper mapper)
        : IRequestHandler<GetAccountByIdQuery, AccountDto>
    {
        public async Task<AccountDto> Handle(GetAccountByIdQuery request, CancellationToken cancellationToken)
        {
            var account = await accountRepository.GetByIdAsync(request.Id);
            if (account == null)
                throw new Exception("Счет не найден");

            return mapper.Map<AccountDto>(account);
        }
    }
}
