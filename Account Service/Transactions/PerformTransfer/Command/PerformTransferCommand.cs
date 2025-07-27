using MediatR;

namespace Account_Service.Transactions.PerformTransfer.Command
{
    public class PerformTransferCommand : IRequest<TransactionDto[]>
    {
        public Guid FromAccountId { get; set; }
        public Guid ToAccountId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; }
        public string? Description { get; set; }
    }
}
