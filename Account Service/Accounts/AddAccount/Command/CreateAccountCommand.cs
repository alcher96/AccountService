using MediatR;


namespace Account_Service.Accounts.AddAccount.Command
{
    /// <summary>
    /// command для создания аккаунта
    /// </summary>
    public class CreateAccountCommand : IRequest<MbResult<AccountDto>>
    {
        /// <summary>
        /// Уникальный идентификатор владельца счета
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
        /// Баланс счета
        /// </summary>
        public decimal Balance { get; set; }

        /// <summary>
        /// Процентная ставка (обязательна для Deposit и Credit, игнорируется для Checking).
        /// </summary>
        public decimal? InterestRate { get; set; }
    }
}
