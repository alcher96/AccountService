using MediatR;

namespace Account_Service.Accounts.DeleteAccount.Command
{
    public class DeleteAccountCommand : IRequest
    {
        public Guid Id { get; set; }
    }
}
