using Account_Service.Accounts.PatchAccount.Command;
using Account_Service.Repositories;
using AutoMapper;
using FluentValidation;
using MediatR;

namespace Account_Service.Accounts.PatchAccount
{
    public class PatchAccountCommandHandler(IAccountRepository accountRepository, IMapper mapper)
        : IRequestHandler<PatchAccountCommand, AccountDto>
    {
        public async Task<AccountDto> Handle(PatchAccountCommand request, CancellationToken cancellationToken)
        {
            var account = await accountRepository.GetByIdAsync(request.Id);
            if (account == null)
                throw new ValidationException("Счет не найден");

            if (request.Request.Currency != null && account.Transactions.Count > 0)
                throw new ValidationException("Нельзя изменить валюту счета с существующими транзакциями");

            mapper.Map(request.Request, account);
            await accountRepository.UpdateAsync(account);
            return mapper.Map<AccountDto>(account);
        }
    }
}
