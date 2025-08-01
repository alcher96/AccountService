using MediatR;
#pragma warning disable CS1591 // Избыточный xml комментарий

namespace Account_Service.Accounts.AddAccount.Command
{
    public class CreateAccountCommand : IRequest<MbResult<AccountDto>>
    {
        public Guid OwnerId { get; set; }
        public AccountType AccountType { get; set; }

        public string? Currency { get; set; }
        public decimal Balance { get; set; }
        public decimal? InterestRate { get; set; }
    }
}
