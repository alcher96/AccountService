using MediatR;

namespace Account_Service.Transactions.AddTransaction.Command
{
    public class CreateTransactionCommand : IRequest<TransactionDto>
    {
        public Guid AccountId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; }
        public TransactionType Type { get; set; }
        public string? Description { get; set; }
        public DateTime DateTime { get; set; }
    }
}
