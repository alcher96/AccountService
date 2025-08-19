namespace AccountService.Messaging.Events.Client
{
    /// <summary>
    /// событие блокирования клиента
    /// </summary>
    public class ClientBlockedEvent
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
        /// Guid клиента
        /// </summary>
        public Guid ClientId { get; set; }

        /// <summary>
        /// Метаданные
        /// </summary>
        public MetaData Meta { get; set; } = new()
        {
            CorrelationId = Guid.NewGuid(),
            CausationId = Guid.NewGuid()
        };
    }
}
