// ReSharper disable UnusedMember.Global
#pragma warning disable CS1591 // Избыточный xml комментарий
namespace Account_Service.Transactions
{
    public class TransactionDto
    {
        /// <summary>
        /// Уникальный идентификатор транзакции в формате GUID.
        /// </summary>
        public Guid TransactionId { get; set; }

        /// <summary>
        /// Идентификатор счёта, к которому относится транзакция, в формате GUID.
        /// </summary>
        public Guid AccountId { get; set; }

        /// <summary>
        ///Уникальный идентификатор отправитиля в формате GUID
        /// </summary>
        public Guid? CounterpartyAccountId { get; set; }

        /// <summary>
        /// Сумма транзакции. Должна быть положительной.
        /// </summary>
        public decimal Amount { get; set; }

        /// <summary>
        /// Валюта транзакции USD RUB EUR.
        /// </summary>
        public string? Currency { get; set; }

        /// <summary>
        /// Тип транзакции. Поддерживаемые значения: Deposit, Withdrawal, Transfer.
        /// </summary>
        public TransactionType Type { get; set; }

        /// <summary>
        /// Описание транзакции, например, "Пополнение счёта".
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Дата и время транзакции в формате ISO 8601.
        /// </summary>
        public DateTime DateTime { get; set; }
    }
}
