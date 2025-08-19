// ReSharper disable UnusedMember.Global
namespace AccountService.Messaging.Events.Client
{
    /// <summary>
    /// Событие разблокированного клинта
    /// </summary>
    public class ClientUnblockedEvent
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
        /// метаданные
        /// </summary>
        public MetaData Meta { get; set; } = new()
        {
            CorrelationId = Guid.NewGuid(),
            CausationId = Guid.NewGuid()
        };
    }
}
