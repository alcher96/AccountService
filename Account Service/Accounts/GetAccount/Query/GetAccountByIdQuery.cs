using MediatR;

namespace Account_Service.Accounts.GetAccount.Query
{
    public class GetAccountByIdQuery : IRequest<AccountDto>
    {
        public Guid Id { get; set; }
    }
}

