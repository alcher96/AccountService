using MediatR;
#pragma warning disable CS1591 // Избыточный xml комментарий

namespace Account_Service.Transactions.AddTransaction.Command
{
    public class CreateTransactionCommand : IRequest<MbResult<TransactionDto>>
    {
        public Guid AccountId { get; set; }
        public decimal Amount { get; set; }
        public string? Currency { get; set; }
        public TransactionType Type { get; set; }
        public string? Description { get; set; }
        // ReSharper disable once UnusedMember.Global
        public DateTime DateTime { get; set; }
    }
}
