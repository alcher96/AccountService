using MediatR;

namespace Account_Service.Accounts.UpdateAccount.Command
{
    public class UpdateAccountCommand : IRequest<AccountDto>
    {
        public Guid Id { get; set; }

        public UpdateAccountRequestDto Request { get; set; }
    }
}
