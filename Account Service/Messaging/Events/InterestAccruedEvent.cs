namespace AccountService.Messaging.Events
{
    /// <summary>
    /// Событие увеличение депозита согласно процентной ставке
    /// </summary>
    public class InterestAccruedEvent
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
        /// начисления с периода
        /// </summary>
        public DateTime PeriodFrom { get; set; }
        /// <summary>
        /// начисления по период
        /// </summary>
        public DateTime PeriodTo { get; set; }
        /// <summary>
        /// сумма начисления
        /// </summary>
        public decimal Amount { get; set; }


        /// <summary>
        /// Meta для версионирования
        /// </summary>
        public MetaData Meta { get; set; } = new()
        {
            CorrelationId = Guid.NewGuid(),
            CausationId = Guid.NewGuid()
        };
    }
}
