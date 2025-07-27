using Account_Service.Accounts.AddAccount.Command;
using Account_Service.Repositories;
using AutoMapper;
using MediatR;

namespace Account_Service.Accounts.AddAccount
{
    public class CreateAccountCommandHandler(IAccountRepository accountRepository, IMapper mapper)
        : IRequestHandler<CreateAccountCommand, AccountDto>
    {
        public async Task<AccountDto> Handle(CreateAccountCommand request, CancellationToken cancellationToken)
        {
            var account = mapper.Map<Account>(request);
            await accountRepository.AddAsync(account);
            return mapper.Map<AccountDto>(account);
        }
    }
}
