using MediatR;
#pragma warning disable CS1591 // Избыточный xml комментарий

namespace Account_Service.Accounts.UpdateAccount.Command
{
    public class UpdateAccountCommand : IRequest<MbResult<AccountDto>>
    {
        public Guid Id { get; set; }

        public UpdateAccountRequestDto? Request { get; set; }
    }
}
