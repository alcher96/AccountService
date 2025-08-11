using MediatR;
#pragma warning disable CS1591 // Избыточный xml комментарий

namespace AccountService.Features.Accounts.DeleteAccount.Command
{
    public class DeleteAccountCommand : IRequest<MbResult<Unit>>
    {
        public Guid Id { get; set; }
    }
}
