using System.ComponentModel.DataAnnotations;
using AccountService.Features.Transactions;
// ReSharper disable EntityFramework.ModelValidation.UnlimitedStringLength
// ReSharper disable CommentTypo

namespace AccountService.Features.Accounts
{
    /// <summary>
    /// модель для сущности "счет"
    /// </summary>
    public class Account
    {
        /// <summary>
        /// guid счета
        /// </summary>
        [Key]
        public Guid AccountId { get; set; }
        /// <summary>
        /// guid пользователя
        /// </summary>
        public Guid OwnerId { get; set; }
        /// <summary>
        /// тип счета (Checking,Deposit)
        /// </summary>
        public AccountType AccountType { get; set; } 
        /// <summary>
        /// валюта (RUB, USD, EUR)
        /// </summary>
        public string Currency { get; set; } = null!;
        /// <summary>
        /// баланс счета
        /// </summary>
        public decimal Balance { get; set; }
        /// <summary>
        /// Процентная ставка
        /// </summary>
        public decimal InterestRate { get; set; }
        /// <summary>
        /// дата открытия счета
        /// </summary>
        public DateTime OpeningDate { get; set; }
        
        /// <summary>
        /// дата закрытия счета
        /// </summary>
        public DateTime? ClosingDate { get; set; }


        /// <summary>
        /// блокировка счета
        /// </summary>
        public bool IsFrozen { get; set; } = false;

        /// <summary>
        /// навигационное поле
        /// </summary>
        public List<Transaction> Transactions { get; set; } = [];

        /// <summary>
        /// для обеспечения конкурентности
        /// </summary>
        // будет мапиться на xmin
        public uint RowVersion { get; set; }
    }
}
