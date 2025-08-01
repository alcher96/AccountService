#pragma warning disable CS1591 //Избыточный xml комментарий
namespace Account_Service.Accounts
{
    public class AccountDto
    {
        public Guid AccountId { get; set; }
        public Guid OwnerId { get; set; }
        public AccountType AccountType { get; set; }
        public string? Currency { get; set; }
        public decimal Balance { get; set; }
        public decimal? InterestRate { get; set; }
        public DateTime OpeningDate { get; set; }
        // ReSharper disable once UnusedMember.Global опциональное поле
        public DateTime? ClosingDate { get; set; }
    }
}
