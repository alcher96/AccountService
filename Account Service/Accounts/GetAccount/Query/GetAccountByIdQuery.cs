using MediatR;
#pragma warning disable CS1591 // Избыточный xml комментарий

namespace Account_Service.Accounts.GetAccount.Query
{
    public class GetAccountByIdQuery : IRequest<MbResult<AccountDto>>
    {
        public Guid Id { get; set; }
    }
}

