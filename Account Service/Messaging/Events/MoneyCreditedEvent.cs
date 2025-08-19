namespace AccountService.Messaging.Events
{
    /// <summary>
    /// Событие Credit для счета
    /// </summary>
    public class MoneyCreditedEvent
    {
        /// <summary>
        /// Guid события
        /// </summary>
        public Guid EventId { get; set; } = Guid.NewGuid();
        /// <summary>
        /// Дата события
        /// </summary>
        public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
        /// <summary>
        /// Guid счета
        /// </summary>
        public Guid AccountId { get; set; }
        /// <summary>
        /// Сумма для транзакции
        /// </summary>
        public decimal Amount { get; set; }
        /// <summary>
        /// валюта транзакции
        /// </summary>
        public string Currency { get; set; } = string.Empty;
        /// <summary>
        /// Guid операции
        /// </summary>
        public Guid OperationId { get; set; }

        // Meta для версионирования
        /// <summary>
        /// 
        /// </summary>
        public MetaData Meta { get; set; } = new()
        {
            CorrelationId = Guid.NewGuid(),
            CausationId = Guid.NewGuid()
        };
    }
}
