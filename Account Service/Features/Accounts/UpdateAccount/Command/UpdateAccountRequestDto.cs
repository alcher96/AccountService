#pragma warning disable CS1591 // Избыточный xml комментарий

#pragma warning disable CS1591 // Избыточный xml комментарий
namespace AccountService.Features.Accounts.UpdateAccount.Command
{
    //дто обертка над UpdateAccountCommand чтобы не ломать валидатор и передавать id через url
    public class UpdateAccountRequestDto
    {
        public Guid OwnerId { get; set; }
        public AccountType Type { get; set; }
        public string? Currency { get; set; }
        public decimal? InterestRate { get; set; }
    }
}
