using MediatR;
#pragma warning disable CS1591 // Избыточный xml комментарий

namespace Account_Service.Transactions.GetAccountTransactions.Query
{
    public class GetAccountTransactionsQuery : IRequest<MbResult<List<TransactionDto>>>
    {
        public Guid AccountId { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }
}
