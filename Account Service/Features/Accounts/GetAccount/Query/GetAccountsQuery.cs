using MediatR;
#pragma warning disable CS1591 // Избыточный xml комментарий

namespace AccountService.Features.Accounts.GetAccount.Query
{
    public class GetAccountsQuery : IRequest<MbResult<List<AccountDto>>>
    {
        public Guid? OwnerId { get; set; }
        public AccountType? Type { get; set; }
    }
}
