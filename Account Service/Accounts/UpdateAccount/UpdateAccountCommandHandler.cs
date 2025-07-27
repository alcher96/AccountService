using Account_Service.Accounts.UpdateAccount.Command;
using Account_Service.Repositories;
using AutoMapper;
using MediatR;

namespace Account_Service.Accounts.UpdateAccount
{
    public class UpdateAccountCommandHandler(IAccountRepository accountRepository, IMapper mapper)
        : IRequestHandler<UpdateAccountCommand, AccountDto>
    {
        public async Task<AccountDto> Handle(UpdateAccountCommand command, CancellationToken cancellationToken)
        {
            var account = await accountRepository.GetByIdAsync(command.Id);
            mapper.Map(command.Request, account);
            await accountRepository.UpdateAsync(account);
            return mapper.Map<AccountDto>(account);
        }
    }
}
