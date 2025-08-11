using MediatR;
#pragma warning disable CS1591 // Избыточный xml комментарий

namespace AccountService.Features.Transactions.AddTransaction.Command
{
    public class CreateTransactionCommand : IRequest<MbResult<TransactionDto>>
    {
        /// <summary>
        /// Идентификатор счёта, к которому относится транзакция, в формате GUID.
        /// </summary>
        public Guid AccountId { get; set; }

        /// <summary>
        /// Сумма транзакции. Должна быть положительной.
        /// </summary>
        public decimal Amount { get; set; }

        /// <summary>
        /// Валюта USD RUB EUR.
        /// </summary>
        public string? Currency { get; set; }

        /// <summary>
        /// Тип транзакции. Поддерживаемые значения: Deposit, Credit.
        /// </summary>
        public TransactionType Type { get; set; }

        /// <summary>
        /// Описание транзакции, например, "Пополнение счёта".
        /// </summary>
        public string? Description { get; set; }
        // ReSharper disable once UnusedMember.Global
        public DateTime DateTime { get; set; }
    }
}
