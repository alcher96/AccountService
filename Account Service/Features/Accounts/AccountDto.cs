// ReSharper disable UnusedMember.Global
namespace AccountService.Features.Accounts
{
    /// <summary>
    /// ДТО для счета 
    /// </summary>
    public class AccountDto
    {
        /// <summary>
        /// идентификатор счета
        /// </summary>
        public Guid AccountId { get; set; }
        /// <summary>
        /// Уникальный идентификатор владельца счёта в формате GUID.
        /// </summary>
        public Guid OwnerId { get; set; }

        /// <summary>
        /// Тип счёта. Поддерживаемые значения: Checking, Deposit, Credit.
        /// </summary>
        public AccountType AccountType { get; set; }

        /// <summary>
        /// Валюта счёта. Поддерживаемые значения: RUB, USD, EUR.
        /// </summary>
        public string? Currency { get; set; }
        /// <summary>
        /// Текущий баланс счёта.
        /// </summary>
        public decimal Balance { get; set; }

        /// <summary>
        /// Процентная ставка для счёта (обязательна для Deposit и Credit, 0 для Checking).
        /// </summary>
        public decimal? InterestRate { get; set; }

        /// <summary>
        /// Дата открытия счёта в формате ISO 8601.
        /// </summary>
        public DateTime OpeningDate { get; set; }
        /// <summary>
        ///  Дата закрытия счёта в формате ISO 8601, или null, если счёт активен.
        /// </summary>
        public DateTime? ClosingDate { get; set; }

        /// <summary>
        /// блокировка счета
        /// </summary>
        public bool IsFrozen { get; set; }
    }
}
