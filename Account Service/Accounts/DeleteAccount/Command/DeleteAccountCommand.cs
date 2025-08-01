using MediatR;
#pragma warning disable CS1591 // Избыточный xml комментарий

namespace Account_Service.Accounts.DeleteAccount.Command
{
    public class DeleteAccountCommand : IRequest<MbResult<Unit>>
    {
        public Guid Id { get; set; }
    }
}
