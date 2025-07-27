using MediatR;

namespace Account_Service.Transactions.GetAccountTransactions.Query
{
    public class GetAccountTransactionsQuery : IRequest<List<TransactionDto>>
    {
        public Guid AccountId { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }
}
