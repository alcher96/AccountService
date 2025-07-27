using MediatR;

namespace Account_Service.Accounts.PatchAccount.Command
{
    public class PatchAccountCommand : IRequest<AccountDto>
    {
        public Guid Id { get; set; }
      
        public PatchAccountRequestDto Request { get; set; }
    }
}
