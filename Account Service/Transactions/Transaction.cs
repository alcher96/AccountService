#pragma warning disable CS1591 // Избыточный xml комментарий
namespace Account_Service.Transactions
{
    public class Transaction
    {
        public Guid TransactionId { get; set; }
        public Guid AccountId { get; set; }
        public Guid? CounterpartyAccountId { get; set; }
        // ReSharper disable once UnusedMember.Global
        public decimal Amount { get; set; }
        // ReSharper disable once UnusedMember.Global
        public string? Currency { get; set; }

        public TransactionType Type { get; set; }

        // ReSharper disable once UnusedMember.Global
        public string? Description { get; set; }
        public DateTime DateTime { get; set; }
    }
}
