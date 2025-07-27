using Account_Service.Accounts.DeleteAccount.Command;
using Account_Service.Repositories;
using MediatR;

namespace Account_Service.Accounts.DeleteAccount
{
    public class DeleteAccountCommandHandler(IAccountRepository accountRepository)
        : IRequestHandler<DeleteAccountCommand>
    {
        public async Task Handle(DeleteAccountCommand request, CancellationToken cancellationToken)
        {
            var account = await accountRepository.GetByIdAsync(request.Id);
            if (account == null)
                throw new Exception("Счет не найден");

            await accountRepository.DeleteAsync(request.Id);
            
        }
    }
}
