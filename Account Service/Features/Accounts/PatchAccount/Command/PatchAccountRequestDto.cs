#pragma warning disable CS1591 //Избыточный xml комментарий

#pragma warning disable CS1591 //Избыточный xml комментарий
namespace AccountService.Features.Accounts.PatchAccount.Command
{
    public class PatchAccountRequestDto
    {
        public string? Currency { get; set; }
        public AccountType? Type { get; set; }
        public decimal? InterestRate { get; set; }
        public decimal? Balance { get; set; }
    }
}
