using MediatR;
#pragma warning disable CS1591 // Избыточный xml комментарий

namespace AccountService.Features.Accounts.GetAccount.Query
{
    public class GetAccountByIdQuery : IRequest<MbResult<AccountDto>>
    {
        public Guid Id { get; set; }
    }
}

