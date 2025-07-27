using MediatR;

namespace Account_Service.Accounts.GetAccount.Query
{
    public class GetAccountsQuery : IRequest<List<AccountDto>>
    {
        public Guid? OwnerId { get; set; }
        public AccountType? Type { get; set; }
    }
}
