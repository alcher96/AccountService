using MediatR;
#pragma warning disable CS1591 // Избыточный xml комментарий

namespace AccountService.Features.Accounts.PatchAccount.Command
{
    public class PatchAccountCommand : IRequest<MbResult<AccountDto>>
    {
        public Guid Id { get; set; }
      
        public PatchAccountRequestDto? Request { get; set; }
    }
}
