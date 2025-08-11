using System.ComponentModel.DataAnnotations;
// ReSharper disable EntityFramework.ModelValidation.UnlimitedStringLength

namespace AccountService.Features.Transactions
{
    /// <summary>
    /// Модель для сущности "транзакция"
    /// </summary>
    public class Transaction
    {
        /// <summary>
        /// guid транзакции
        /// </summary>
        [Key]
        public Guid TransactionId { get; set; }
        /// <summary>
        /// guid счета
        /// </summary>
        public Guid AccountId { get; set; }

        /// <summary>
        /// guid счета с которого проводилась транзакция
        /// </summary>
        public Guid? CounterpartyAccountId { get; set; }
        /// <summary>
        /// сумма транзакции
        /// </summary>
        public decimal Amount { get; set; }
        
        /// <summary>
        /// валюта транзакции
        /// </summary>
        public string? Currency { get; set; }

        /// <summary>
        /// тип транзакции(Credit, Deposit)
        /// </summary>
        public TransactionType Type { get; set; }

        /// <summary>
        /// описание
        /// </summary>
        public string? Description { get; set; }
        /// <summary>
        /// дата транзакции
        /// </summary>
        public DateTime DateTime { get; set; }
    }
}
