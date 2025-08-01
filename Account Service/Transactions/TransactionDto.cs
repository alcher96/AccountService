#pragma warning disable CS1591 // Избыточный xml комментарий
namespace Account_Service.Transactions
{
    public class TransactionDto
    {
        public Guid TransactionId { get; set; }
        public Guid AccountId { get; set; }
        // ReSharper disable once UnusedMember.Global
        public Guid? CounterpartyAccountId { get; set; }
        public decimal Amount { get; set; }
        public string? Currency { get; set; }
        public TransactionType Type { get; set; }
        public string? Description { get; set; }
        public DateTime DateTime { get; set; }
    }
}
