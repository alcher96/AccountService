namespace AccountService.Messaging.Events
{
    /// <summary>
    /// Событие открытия счета
    /// </summary>
    public class AccountOpenedEvent
    {
        /// <summary>
        /// Guid события
        /// </summary>
        public Guid EventId { get; set; } = Guid.NewGuid();
        /// <summary>
        /// дата события
        /// </summary>
        public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
        /// <summary>
        /// Guid счета
        /// </summary>
        public Guid AccountId { get; set; }
        /// <summary>
        /// Guid вледельца счета
        /// </summary>
        public Guid OwnerId { get; set; }
        /// <summary>
        /// Валюта
        /// </summary>
        public string Currency { get; set; } = string.Empty;
        /// <summary>
        /// тип события
        /// </summary>
        public string Type { get; set; } = string.Empty;

        
        /// <summary>
        /// Meta для версионирования
        /// </summary>
        // ReSharper disable once UnusedMember.Global
        public MetaData Meta { get; set; } = new()
        {
            CorrelationId = Guid.NewGuid(),
            CausationId = Guid.NewGuid()
        };


    }
}
