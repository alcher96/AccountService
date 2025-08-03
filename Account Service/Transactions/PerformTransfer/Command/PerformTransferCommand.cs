using MediatR;
#pragma warning disable CS1591 // Избыточный xml комментарий

namespace Account_Service.Transactions.PerformTransfer.Command
{
    public class PerformTransferCommand : IRequest<MbResult<TransactionDto[]>>
    {
        /// <summary>
        /// Счет с которого производится транзакция.
        /// </summary>
        public Guid FromAccountId { get; set; }

        /// <summary>
        /// Счет на который производится транзакция.
        /// </summary>
        public Guid ToAccountId { get; set; }

        /// <summary>
        /// Сумма перевода.
        /// </summary>
        public decimal Amount { get; set; }

        /// <summary>
        /// Валюта перевода RUB EUR USD.
        /// </summary>
        public string? Currency { get; set; }
        /// <summary>
        /// Описание транзакции.
        /// </summary>
        public string? Description { get; set; }
    }
}
